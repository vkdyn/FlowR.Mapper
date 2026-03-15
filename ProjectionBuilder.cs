using FlowR.Mapper.Internal;
using System.Linq.Expressions;
using System.Reflection;

namespace FlowR.Mapper;

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

                // Deep nested mapping
                if (!IsSimpleType(destProp.PropertyType) && !IsSimpleType(sourceProp.PropertyType)
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
}
