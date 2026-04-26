using System.Linq.Expressions;
using System.Reflection;

namespace FlowR.Mapper.Internal;

/// <summary>
/// Builds LINQ projection expressions for use with IQueryable (EF Core, Dapper, etc.)
/// Only selects the columns needed — generates efficient SQL.
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

            // Custom resolver (lambda only — can't use arbitrary Funcs in expression trees)
            if (config.MemberResolvers.TryGetValue(destProp.Name, out var resolver))
            {
                // Wrap resolver in a constant + invoke — works for EF Core in-memory eval
                var resolverConst = Expression.Constant(resolver);
                var invokeExpr = Expression.Invoke(resolverConst, sourceExpr);
                valueExpr = Expression.Convert(invokeExpr, destProp.PropertyType);
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

    /// <summary>
    /// Builds an expression equivalent to: <c>src.SourceCollection.Select(x =&gt; new TDest { ... }).ToList()</c>
    /// (or .ToArray() / .ToHashSet() depending on destination type). Returns null if no element-type mapping
    /// can be resolved and the collections aren't directly assignable.
    /// </summary>
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
        {
            return null;
        }

        // Build per-element projection expression: x => new TDest { ... } or x => x (when types match)
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
            // Element types are compatible without an explicit map (e.g. base/derived)
            Expression body = sourceElementType == destElementType
                ? (Expression)elementParam
                : Expression.Convert(elementParam, destElementType);
            elementSelector = Expression.Lambda(body, elementParam);
        }
        else
        {
            // No way to project elements safely.
            return null;
        }

        // Source as IEnumerable<TSourceElement> for Enumerable.Select.
        // EF Core will translate Select+ToList over a navigation property to SQL.
        var sourceAsEnumerable = sourceCollectionExpr;
        var enumerableOfSource = typeof(IEnumerable<>).MakeGenericType(sourceElementType);
        if (!enumerableOfSource.IsAssignableFrom(sourceCollectionExpr.Type))
        {
            sourceAsEnumerable = Expression.Convert(sourceCollectionExpr, enumerableOfSource);
        }

        // Enumerable.Select<TSource, TResult>(IEnumerable<TSource>, Func<TSource, TResult>)
        var selectCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            new[] { sourceElementType, destElementType },
            sourceAsEnumerable,
            elementSelector);

        // Materialize into the requested destination collection type.
        return BuildMaterialization(selectCall, destElementType, destCollectionType);
    }

    private static Expression? BuildMaterialization(
        Expression selectCall,
        Type destElementType,
        Type destCollectionType)
    {
        // Array
        if (destCollectionType.IsArray)
        {
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.ToArray),
                new[] { destElementType }, selectCall);
        }

        // Concrete List<T> or anything assignable from List<T>
        var listType = typeof(List<>).MakeGenericType(destElementType);
        if (destCollectionType.IsAssignableFrom(listType))
        {
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList),
                new[] { destElementType }, selectCall);
        }

        // HashSet<T> or anything assignable from HashSet<T>
        var hashSetType = typeof(HashSet<>).MakeGenericType(destElementType);
        if (destCollectionType.IsAssignableFrom(hashSetType))
        {
            // Enumerable.ToHashSet<T>(IEnumerable<T>) — available in .NET Core 2.0+ / netstandard2.1+
            var toHashSet = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == nameof(Enumerable.ToHashSet)
                                     && m.GetParameters().Length == 1);
            if (toHashSet != null)
            {
                return Expression.Call(toHashSet.MakeGenericMethod(destElementType), selectCall);
            }
            // Fallback: new HashSet<T>(IEnumerable<T>)
            var ctor = hashSetType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(destElementType) });
            if (ctor != null) return Expression.New(ctor, selectCall);
        }

        // Concrete collection with IEnumerable<T> constructor (e.g. Collection<T>, ObservableCollection<T>)
        if (!destCollectionType.IsInterface && !destCollectionType.IsAbstract)
        {
            var ctor = destCollectionType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(destElementType) });
            if (ctor != null) return Expression.New(ctor, selectCall);
        }

        // Last resort: ToList() and rely on assignment compatibility (covers IEnumerable<T>, ICollection<T>, IList<T>, IReadOnlyList<T>, etc.)
        var toListCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList),
            new[] { destElementType }, selectCall);
        return destCollectionType.IsAssignableFrom(toListCall.Type)
            ? toListCall
            : (Expression)Expression.Convert(toListCall, destCollectionType);
    }
}