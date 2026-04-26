using System.Linq.Expressions;
using System.Reflection;

namespace FlowR.Mapper.Internal;

/// <summary>
/// Builds LINQ projection expressions for use with IQueryable (EF Core, Dapper, etc.)
/// Only selects the columns needed — generates efficient SQL.
/// Members with Func-only resolvers are skipped here and applied post-query by FlowRMapper.
/// </summary>
internal static class ProjectionBuilder
{
    public static Expression<Func<TSource, TDestination>> BuildProjection<TSource, TDestination>(
        MappingConfiguration config,
        MapperRegistry registry)
    {
        var sourceParam = Expression.Parameter(typeof(TSource), "src");
        var bindings = BuildBindings(typeof(TSource), typeof(TDestination), sourceParam, config, registry, depth: 0);

        var body = Expression.MemberInit(
            Expression.New(typeof(TDestination)),
            bindings);

        return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParam);
    }

    /// <summary>
    /// Returns the set of destination member names that have Func-only resolvers
    /// and were therefore skipped in the SQL projection — caller must apply them post-query.
    /// </summary>
    public static HashSet<string> GetPostQueryMembers(MappingConfiguration config)
    {
        var result = new HashSet<string>();
        foreach (var key in config.MemberResolvers.Keys)
        {
            if (!config.MemberExpressions.ContainsKey(key))
                result.Add(key);
        }
        return result;
    }

    private static List<MemberBinding> BuildBindings(
        Type sourceType, Type destType,
        Expression sourceExpr,
        MappingConfiguration config,
        MapperRegistry registry,
        int depth)
    {
        if (depth > config.MaxDepth) return [];

        var sourceProps = sourceType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name);

        var destProps = destType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        var bindings = new List<MemberBinding>();

        foreach (var destProp in destProps)
        {
            if (config.IgnoredMembers.Contains(destProp.Name)) continue;

            Expression? valueExpr = null;

            // Expression-based resolver — fully inlined, EF Core can translate to SQL
            if (config.MemberExpressions.TryGetValue(destProp.Name, out var memberExpr))
            {
                valueExpr = ExpressionParameterReplacer.Replace(
                    memberExpr.Body, memberExpr.Parameters[0], sourceExpr);
                if (valueExpr.Type != destProp.PropertyType)
                    valueExpr = Expression.Convert(valueExpr, destProp.PropertyType);
            }
            // Func-only resolver — skip in SQL projection, applied post-query by FlowRMapper
            else if (config.MemberResolvers.ContainsKey(destProp.Name))
            {
                continue;
            }
            // Constant value
            else if (config.MemberConstants.TryGetValue(destProp.Name, out var constant))
            {
                valueExpr = Expression.Constant(constant, destProp.PropertyType);
            }
            // Name match
            else if (sourceProps.TryGetValue(destProp.Name, out var sourceProp))
            {
                var sourcePropExpr = Expression.Property(sourceExpr, sourceProp);

                // Collection-to-collection projection: src.Items.Select(i => new TDest { ... }).ToList()
                if (IsCollectionType(sourceProp.PropertyType) && IsCollectionType(destProp.PropertyType))
                {
                    valueExpr = TryBuildCollectionProjection(
                        sourcePropExpr,
                        sourceProp.PropertyType,
                        destProp.PropertyType,
                        registry,
                        depth);
                }
                // Deep nested mapping
                else if (!IsSimpleType(destProp.PropertyType) && !IsSimpleType(sourceProp.PropertyType)
                    && registry.Has(sourceProp.PropertyType, destProp.PropertyType))
                {
                    var nestedConfig = registry.Get(sourceProp.PropertyType, destProp.PropertyType)!;
                    var nestedBindings = BuildBindings(sourceProp.PropertyType, destProp.PropertyType,
                        sourcePropExpr, nestedConfig, registry, depth + 1);
                    valueExpr = Expression.MemberInit(Expression.New(destProp.PropertyType), nestedBindings);
                }
                else
                {
                    valueExpr = sourcePropExpr.Type == destProp.PropertyType
                        ? (Expression)sourcePropExpr
                        : Expression.Convert(sourcePropExpr, destProp.PropertyType);
                }
            }
            // Flattening
            else if (config.FlattenEnabled)
            {
                valueExpr = TryBuildFlattenedExpression(sourceProps, sourceExpr, destProp.Name, destProp.PropertyType);
            }

            if (valueExpr == null) continue;

            bindings.Add(Expression.Bind(destProp, valueExpr));
        }

        return bindings;
    }

    private static Expression? TryBuildFlattenedExpression(
        Dictionary<string, PropertyInfo> sourceProps,
        Expression sourceExpr,
        string destMemberName,
        Type destPropType)
    {
        foreach (var prop in sourceProps.Values)
        {
            if (!destMemberName.StartsWith(prop.Name, StringComparison.OrdinalIgnoreCase)) continue;

            var remainingName = destMemberName[prop.Name.Length..];
            var nestedExpr = Expression.Property(sourceExpr, prop);

            if (string.IsNullOrEmpty(remainingName))
                return nestedExpr.Type == destPropType ? nestedExpr : Expression.Convert(nestedExpr, destPropType);

            var nestedProps = prop.PropertyType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name);

            return TryBuildFlattenedExpression(nestedProps, nestedExpr, remainingName, destPropType);
        }
        return null;
    }

    private static bool IsSimpleType(Type type) =>
        type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid) ||
        type == typeof(TimeSpan) || type.IsEnum || Nullable.GetUnderlyingType(type) != null;

    private static bool IsCollectionType(Type type)
    {
        if (type == typeof(string)) return false;
        if (type.IsArray) return true;
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray) return collectionType.GetElementType();

        if (collectionType.IsGenericType
            && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return collectionType.GetGenericArguments()[0];
        }

        return collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static Expression? TryBuildCollectionProjection(
        Expression sourceCollectionExpr,
        Type sourceCollectionType,
        Type destCollectionType,
        MapperRegistry registry,
        int depth)
    {
        var sourceElementType = GetCollectionElementType(sourceCollectionType);
        var destElementType = GetCollectionElementType(destCollectionType);

        if (sourceElementType == null || destElementType == null)
            return null;

        Expression elementSelector;
        var elementParam = Expression.Parameter(sourceElementType, "x");

        if (sourceElementType == destElementType)
        {
            elementSelector = Expression.Lambda(elementParam, elementParam);
        }
        else if (registry.Has(sourceElementType, destElementType))
        {
            var elementConfig = registry.Get(sourceElementType, destElementType)!;
            var elementBindings = BuildBindings(
                sourceElementType, destElementType, elementParam, elementConfig, registry, depth + 1);
            var elementBody = Expression.MemberInit(Expression.New(destElementType), elementBindings);
            elementSelector = Expression.Lambda(elementBody, elementParam);
        }
        else if (destElementType.IsAssignableFrom(sourceElementType))
        {
            Expression body = sourceElementType == destElementType
                ? (Expression)elementParam
                : Expression.Convert(elementParam, destElementType);
            elementSelector = Expression.Lambda(body, elementParam);
        }
        else
        {
            return null;
        }

        var enumerableOfSource = typeof(IEnumerable<>).MakeGenericType(sourceElementType);
        var sourceAsEnumerable = enumerableOfSource.IsAssignableFrom(sourceCollectionExpr.Type)
            ? sourceCollectionExpr
            : Expression.Convert(sourceCollectionExpr, enumerableOfSource);

        var selectCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            new[] { sourceElementType, destElementType },
            sourceAsEnumerable,
            elementSelector);

        return BuildMaterialization(selectCall, destElementType, destCollectionType);
    }

    private static Expression? BuildMaterialization(
        Expression selectCall,
        Type destElementType,
        Type destCollectionType)
    {
        if (destCollectionType.IsArray)
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.ToArray),
                new[] { destElementType }, selectCall);

        var listType = typeof(List<>).MakeGenericType(destElementType);
        if (destCollectionType.IsAssignableFrom(listType))
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList),
                new[] { destElementType }, selectCall);

        var hashSetType = typeof(HashSet<>).MakeGenericType(destElementType);
        if (destCollectionType.IsAssignableFrom(hashSetType))
        {
            var toHashSet = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == nameof(Enumerable.ToHashSet) && m.GetParameters().Length == 1);
            if (toHashSet != null)
                return Expression.Call(toHashSet.MakeGenericMethod(destElementType), selectCall);

            var ctor = hashSetType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(destElementType) });
            if (ctor != null) return Expression.New(ctor, selectCall);
        }

        if (!destCollectionType.IsInterface && !destCollectionType.IsAbstract)
        {
            var ctor = destCollectionType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(destElementType) });
            if (ctor != null) return Expression.New(ctor, selectCall);
        }

        var toListCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList),
            new[] { destElementType }, selectCall);
        return destCollectionType.IsAssignableFrom(toListCall.Type)
            ? toListCall
            : (Expression)Expression.Convert(toListCall, destCollectionType);
    }
}

/// <summary>
/// Replaces a specific parameter in an expression tree with another expression.
/// Used to inline lambda bodies directly into projection trees for EF Core SQL translation.
/// </summary>
internal sealed class ExpressionParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _target;
    private readonly Expression _replacement;

    private ExpressionParameterReplacer(ParameterExpression target, Expression replacement)
    {
        _target = target;
        _replacement = replacement;
    }

    public static Expression Replace(Expression body, ParameterExpression target, Expression replacement)
        => new ExpressionParameterReplacer(target, replacement).Visit(body)!;

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _target ? _replacement : base.VisitParameter(node);
}