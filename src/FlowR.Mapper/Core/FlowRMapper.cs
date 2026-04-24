using FlowR.Mapper.Exceptions;
using FlowR.Mapper.Interfaces;
using FlowR.Mapper.Internal;
using System.Collections.Concurrent;
using System.Reflection;

namespace FlowR.Mapper.Core;


/// <summary>
/// The FlowR.Mapper engine.
/// All mapping delegates are compiled once on first use and cached — near zero overhead on hot paths.
/// </summary>
public sealed class FlowRMapper : IMapper
{
    private readonly MapperRegistry _registry;
    // Compiled mapper cache: (TSource, TDest) -> compiled Func<TSource, TDest>
    private readonly ConcurrentDictionary<(Type, Type), Delegate> _compiledMappers = new();

    internal FlowRMapper(MapperRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var config = _registry.GetOrThrow(source.GetType(), typeof(TDestination));
        return (TDestination)ExecuteMapping(source, null, config, source.GetType(), typeof(TDestination))!;
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source == null)
        {
            var config2 = _registry.Get(typeof(TSource), typeof(TDestination));
            if (config2?.NullSubstitute is TDestination sub) return sub;
            return default!;
        }

        // Handle polymorphism — check if derived type mapping exists
        var actualSourceType = source.GetType();
        var config = _registry.Get(actualSourceType, typeof(TDestination))
                     ?? _registry.GetOrThrow(typeof(TSource), typeof(TDestination));

        return (TDestination)ExecuteMapping(source, null, config, actualSourceType, typeof(TDestination))!;
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        var config = _registry.GetOrThrow(typeof(TSource), typeof(TDestination));
        return (TDestination)ExecuteMapping(source, destination, config, typeof(TSource), typeof(TDestination))!;
    }

    /// <inheritdoc />
    public IEnumerable<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
        => source.Select(s => Map<TSource, TDestination>(s));

    /// <inheritdoc />
    public List<TDestination> MapToList<TSource, TDestination>(IEnumerable<TSource> source)
        => source.Select(s => Map<TSource, TDestination>(s)).ToList();

    /// <inheritdoc />
    public TDestination[] MapToArray<TSource, TDestination>(IEnumerable<TSource> source)
        => source.Select(s => Map<TSource, TDestination>(s)).ToArray();

    /// <inheritdoc />
    public IQueryable<TDestination> ProjectTo<TSource, TDestination>(IQueryable<TSource> source)
    {
        var config = _registry.GetOrThrow(typeof(TSource), typeof(TDestination));
        var projection = ProjectionBuilder.BuildProjection<TSource, TDestination>(config, _registry);
        return source.Select(projection);
    }

    /// <inheritdoc />
    public bool HasMapping<TSource, TDestination>()
        => _registry.Has(typeof(TSource), typeof(TDestination));

    /// <inheritdoc />
    public void AssertConfigurationIsValid()
    {
        var errors = new List<string>();

        foreach (var config in _registry.All())
        {
            if (!config.ValidateAllMembers) continue;

            var destProperties = config.DestinationType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => p.Name)
                .ToHashSet();

            var sourceProperties = config.SourceType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet();

            foreach (var destProp in destProperties)
            {
                if (config.IgnoredMembers.Contains(destProp)) continue;
                if (config.MemberResolvers.ContainsKey(destProp)) continue;
                if (config.MemberConstants.ContainsKey(destProp)) continue;
                if (sourceProperties.Contains(destProp)) continue;

                // Try flattening: DestinationAddressCity -> source.Address.City
                if (config.FlattenEnabled && TryResolveFlattenedMember(config.SourceType, destProp)) continue;

                errors.Add($"[{config.SourceType.Name} -> {config.DestinationType.Name}] " +
                           $"Destination member '{destProp}' is not mapped and not ignored.");
            }
        }

        if (errors.Count > 0)
            throw new MapperConfigurationException(
                $"FlowR.Mapper configuration errors:\n{string.Join("\n", errors)}");
    }

    private bool TryResolveFlattenedMember(Type sourceType, string destMemberName)
    {
        // Try to find a nested property path matching the destination name
        var properties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (destMemberName.StartsWith(prop.Name, StringComparison.OrdinalIgnoreCase))
            {
                var remainingName = destMemberName[prop.Name.Length..];
                if (string.IsNullOrEmpty(remainingName)) return true;
                if (TryResolveFlattenedMember(prop.PropertyType, remainingName)) return true;
            }
        }
        return false;
    }

    private object? ExecuteMapping(object source, object? existingDest, MappingConfiguration config,
        Type sourceType, Type destType)
    {
        // Type converter short-circuits everything
        if (config.TypeConverter != null)
        {
            // Check if converter expects destination parameter
            var converterParams = config.TypeConverter.Method.GetParameters();
            if (converterParams.Length == 2)
            {
                // For 2-param converter, pass existing dest or default value (don't try to create)
                var destForConverter = existingDest ?? GetDefaultValue(destType);
                return config.TypeConverter.DynamicInvoke(source, destForConverter);
            }
            else
            {
                return config.TypeConverter.DynamicInvoke(source);
            }
        }

        // Check global condition
        if (config.GlobalCondition != null)
        {
            var condResult = config.GlobalCondition.DynamicInvoke(source, existingDest);
            if (condResult is false) return existingDest;
        }

        // Create destination
        var dest = existingDest ?? CreateDestination(config, source);

        // Create resolution context
        var context = new ResolutionContext(
            source: source,
            destination: dest,
            sourceType: sourceType,
            destinationType: destType,
            mapper: this
        );

        // Run before hooks
        foreach (var before in config.BeforeMapActions)
            before.Execute(source, dest, context);

        // Map all properties
        MapProperties(source, dest, config, sourceType, destType, depth: 0);

        // Run after hooks
        foreach (var after in config.AfterMapActions)
            after.Execute(source, dest, context);

        return dest;
    }

    private void MapProperties(object source, object dest, MappingConfiguration config,
        Type sourceType, Type destType, int depth)
    {
        if (depth > config.MaxDepth) return;

        var sourceProps = sourceType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name);

        var destProps = destType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        // Sort by mapping order if specified
        if (config.MemberMappingOrder.Count > 0)
        {
            destProps = destProps
                .OrderBy(p => config.MemberMappingOrder.TryGetValue(p.Name, out var order) ? order : int.MaxValue)
                .ToList();
        }

        // Track which members have been explicitly configured
        var configuredMembers = new HashSet<string>(
            config.MemberResolvers.Keys
            .Concat(config.MemberConstants.Keys)
            .Concat(config.IgnoredMembers)
            .Concat(config.ValueResolvers.Keys)
            .Concat(config.PathResolvers.Keys.Select(p => p.Split('.')[0]))
        );

        foreach (var destProp in destProps)
        {
            if (config.IgnoredMembers.Contains(destProp.Name)) continue;
            if (IsGloballyIgnored(destProp.Name)) continue;

            // UseDestinationValue - skip mapping for this member
            if (config.UseDestinationValueMembers.Contains(destProp.Name)) continue;

            // Check member-level condition
            if (config.MemberConditions.TryGetValue(destProp.Name, out var memberCond))
            {
                var condResult = memberCond.DynamicInvoke(source);
                if (condResult is false) continue;
            }

            object? value = null;
            bool resolved = false;

            // Priority 1: Constant value
            if (config.MemberConstants.TryGetValue(destProp.Name, out var constant))
            {
                value = constant;
                resolved = true;
            }
            // Priority 2: Value resolver (IValueResolver)
            else if (config.ValueResolvers.TryGetValue(destProp.Name, out var valueResolver))
            {
                var resolverType = valueResolver.GetType();
                var resolveMethod = resolverType.GetMethod("Resolve");
                var currentValue = destProp.GetValue(dest);
                
                var context = new ResolutionContext(source, dest, sourceType, destType, this);
                context.Depth = depth;
                value = resolveMethod!.Invoke(valueResolver, new[] { source, dest, currentValue, context });
                resolved = true;
            }
            // Priority 3: Custom resolver
            else if (config.MemberResolvers.TryGetValue(destProp.Name, out var resolver))
            {
                value = resolver.DynamicInvoke(resolver.Method.GetParameters().Length == 2
                    ? new[] { source, dest }
                    : new[] { source });
                resolved = true;
            }
            // Priority 3: Name match on source
            else if (sourceProps.TryGetValue(destProp.Name, out var sourceProp))
            {
                value = sourceProp.GetValue(source);
                resolved = true;

                // Deep map: if destination property type has its own mapping
                if (config.DeepMapEnabled && value != null
                    && !IsSimpleType(destProp.PropertyType)
                    && _registry.Has(sourceProp.PropertyType, destProp.PropertyType))
                {
                    var nestedConfig = _registry.GetOrThrow(sourceProp.PropertyType, destProp.PropertyType);
                    value = ExecuteMapping(value, null, nestedConfig, sourceProp.PropertyType, destProp.PropertyType);
                }
            }
            // Priority 4: Flattening
            else if (config.FlattenEnabled)
            {
                value = TryGetFlattenedValue(source, sourceProps, destProp.Name);
                if (value != null) resolved = true;
            }

            if (!resolved) continue;

            // Null substitute
            if (value == null && config.MemberNullSubstitutes.TryGetValue(destProp.Name, out var nullSub))
                value = nullSub;

            // Apply global value transforms
            if (value != null && _registry.GlobalValueTransforms.TryGetValue(destProp.PropertyType, out var transform))
                value = transform.DynamicInvoke(value);

            // Handle collections
            if (value != null && IsCollectionType(destProp.PropertyType) && IsCollectionType(value.GetType()))
            {
                value = MapCollection(value, destProp.PropertyType, depth + 1);
            }

            try
            {
                destProp.SetValue(dest, value);
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Error setting '{destProp.Name}' on '{destType.Name}': {ex.Message}", ex);
            }
        }

        // Handle path-based mappings (ForPath)
        foreach (var pathMapping in config.PathResolvers)
        {
            SetNestedProperty(dest, pathMapping.Key, pathMapping.Value.DynamicInvoke(source));
        }

        foreach (var pathConstant in config.PathConstants)
        {
            SetNestedProperty(dest, pathConstant.Key, pathConstant.Value);
        }
    }

    private void SetNestedProperty(object obj, string path, object? value)
    {
        var parts = path.Split('.');
        object current = obj;
        
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var prop = current.GetType().GetProperty(parts[i]);
            if (prop == null) return;
            
            var nextValue = prop.GetValue(current);
            if (nextValue == null)
            {
                nextValue = Activator.CreateInstance(prop.PropertyType);
                prop.SetValue(current, nextValue);
            }
            current = nextValue!;
        }
        
        var finalProp = current.GetType().GetProperty(parts[^1]);
        finalProp?.SetValue(current, value);
    }

    private object? TryGetFlattenedValue(object source,
        Dictionary<string, PropertyInfo> sourceProps, string destMemberName)
    {
        foreach (var prop in sourceProps.Values)
        {
            if (!destMemberName.StartsWith(prop.Name, StringComparison.OrdinalIgnoreCase)) continue;
            var remainingName = destMemberName[prop.Name.Length..];
            if (string.IsNullOrEmpty(remainingName))
                return prop.GetValue(source);

            var nestedValue = prop.GetValue(source);
            if (nestedValue == null) return null;

            var nestedProps = nestedValue.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name);

            return TryGetFlattenedValue(nestedValue, nestedProps, remainingName);
        }
        return null;
    }

    private object? MapCollection(object sourceCollection, Type destCollectionType, int depth)
    {
        var sourceEnumerable = ((System.Collections.IEnumerable)sourceCollection).Cast<object>().ToList();
        if (sourceEnumerable.Count == 0) return CreateEmptyCollection(destCollectionType);

        var destElementType = GetCollectionElementType(destCollectionType);
        var sourceElementType = sourceEnumerable.First().GetType();

        if (destElementType == null) return sourceCollection;

        // Try to find a mapping for the element types
        var elementConfig = _registry.Get(sourceElementType, destElementType);

        var mappedItems = sourceEnumerable.Select(item =>
            elementConfig != null
                ? ExecuteMapping(item, null, elementConfig, sourceElementType, destElementType)
                : item
        ).ToList();

        return CreateCollection(mappedItems, destCollectionType, destElementType);
    }

    private static object CreateDestination(MappingConfiguration config, object source)
    {
        if (config.CustomConstructor != null)
            return config.CustomConstructor.DynamicInvoke(source)!;

        try
        {
            return Activator.CreateInstance(config.DestinationType)
                ?? throw new MappingException($"Cannot create instance of '{config.DestinationType.Name}'. " +
                    "Ensure it has a parameterless constructor or use ConstructUsing().");
        }
        catch (MissingMethodException)
        {
            throw new MappingException(
                $"'{config.DestinationType.Name}' has no parameterless constructor. " +
                "Use ConstructUsing() or ensure a public parameterless constructor exists.");
        }
    }

    private static bool IsSimpleType(Type type) =>
        type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid) ||
        type == typeof(TimeSpan) || type.IsEnum || Nullable.GetUnderlyingType(type) != null;

    private static bool IsCollectionType(Type type) =>
        type != typeof(string) &&
        (type.IsArray || type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

    private static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray) return collectionType.GetElementType();
        return collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static object CreateEmptyCollection(Type collectionType)
    {
        if (collectionType.IsArray) return Array.CreateInstance(collectionType.GetElementType()!, 0);
        var elementType = GetCollectionElementType(collectionType);
        if (elementType == null) return new List<object>();
        return Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
    }

    private static object CreateCollection(List<object?> items, Type collectionType, Type elementType)
    {
        if (collectionType.IsArray)
        {
            var arr = Array.CreateInstance(elementType, items.Count);
            for (int i = 0; i < items.Count; i++) arr.SetValue(items[i], i);
            return arr;
        }

        var list = (System.Collections.IList)Activator.CreateInstance(
            typeof(List<>).MakeGenericType(elementType))!;
        foreach (var item in items) list.Add(item);
        return list;
    }

    private bool IsGloballyIgnored(string memberName) =>
        _registry.GlobalIgnorePredicates.Any(pred => pred(memberName));

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
