using FlowR.Mapper.Configuration;
using FlowR.Mapper.Exceptions;
using FlowR.Mapper.Interfaces;
using FlowR.Mapper.Internal;
using System.Collections.Concurrent;
using System.Reflection;

namespace FlowR.Mapper.Core;

/// <summary>
/// Default FlowR mapper implementation.
/// </summary>
public sealed class FlowRMapper : IMapper
{
    private readonly MapperRegistry _registry;
    private readonly ConcurrentDictionary<MappingConfiguration, bool> _appliedBulkMemberOptions = new();

    internal FlowRMapper(MapperRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Type actualSourceType = source.GetType();
        Type destinationType = typeof(TDestination);

        if (IsCollectionType(actualSourceType) && IsCollectionType(destinationType))
        {
            return (TDestination)MapCollection(source, destinationType, 0)!;
        }

        MappingConfiguration config = _registry.GetOrThrow(actualSourceType, destinationType);
        return (TDestination)ExecuteMapping(source, null, config, actualSourceType, destinationType)!;
    }
    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source == null)
        {
            MappingConfiguration? nullConfig = _registry.Get(typeof(TSource), typeof(TDestination));
            if (nullConfig?.NullSubstitute is TDestination substitute)
            {
                return substitute;
            }

            return default!;
        }

        Type actualSourceType = source.GetType();
        MappingConfiguration config = _registry.Get(actualSourceType, typeof(TDestination))
            ?? _registry.GetOrThrow(typeof(TSource), typeof(TDestination));

        return (TDestination)ExecuteMapping(source, null, config, actualSourceType, typeof(TDestination))!;
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        MappingConfiguration config = _registry.GetOrThrow(typeof(TSource), typeof(TDestination));

        if (source == null)
        {
            if (config.TypeConverter != null)
            {
                return (TDestination)ExecuteMapping(null, destination, config, typeof(TSource), typeof(TDestination))!;
            }

            return destination;
        }

        return (TDestination)ExecuteMapping(source, destination, config, typeof(TSource), typeof(TDestination))!;
    }

    /// <inheritdoc />
    public IEnumerable<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(Map<TSource, TDestination>);
    }

    /// <inheritdoc />
    public List<TDestination> MapToList<TSource, TDestination>(IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(Map<TSource, TDestination>).ToList();
    }

    /// <inheritdoc />
    public TDestination[] MapToArray<TSource, TDestination>(IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(Map<TSource, TDestination>).ToArray();
    }

    /// <inheritdoc />
    public IQueryable<TDestination> ProjectTo<TSource, TDestination>(IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        MappingConfiguration config = _registry.GetOrThrow(typeof(TSource), typeof(TDestination));
        var projection = ProjectionBuilder.BuildProjection<TSource, TDestination>(config, _registry);
        return source.Select(projection);
    }

    /// <inheritdoc />
    public bool HasMapping<TSource, TDestination>()
    {
        return _registry.Has(typeof(TSource), typeof(TDestination));
    }

    /// <inheritdoc />
    public void AssertConfigurationIsValid()
    {
        List<string> errors = [];

        foreach (MappingConfiguration config in _registry.All())
        {
            if (!config.ValidateAllMembers)
            {
                continue;
            }

            HashSet<string> sourceProperties = config.SourceType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead)
                .Select(property => property.Name)
                .ToHashSet();

            foreach (PropertyInfo destinationProperty in GetWritableProperties(config.DestinationType))
            {
                if (config.IgnoredMembers.Contains(destinationProperty.Name))
                {
                    continue;
                }

                if (config.MemberResolvers.ContainsKey(destinationProperty.Name)
                    || config.MemberConstants.ContainsKey(destinationProperty.Name)
                    || config.ValueResolvers.ContainsKey(destinationProperty.Name)
                    || config.PathResolvers.Keys.Any(path => path.Split('.')[0] == destinationProperty.Name)
                    || sourceProperties.Contains(destinationProperty.Name))
                {
                    continue;
                }

                if (config.FlattenEnabled && TryResolveFlattenedMember(config.SourceType, destinationProperty.Name))
                {
                    continue;
                }

                errors.Add($"[{config.SourceType.Name} -> {config.DestinationType.Name}] Destination member '{destinationProperty.Name}' is not mapped and not ignored.");
            }
        }

        if (errors.Count > 0)
        {
            throw new MapperConfigurationException($"FlowR.Mapper configuration errors:\n{string.Join("\n", errors)}");
        }
    }

    private object? ExecuteMapping(
        object? source,
        object? existingDestination,
        MappingConfiguration config,
        Type sourceType,
        Type destinationType)
    {
        ApplyBulkMemberOptions(config);

        if (config.TypeConverter != null)
        {
            ParameterInfo[] parameters = config.TypeConverter.Method.GetParameters();
            if (parameters.Length == 2)
            {
                object? destinationValue = existingDestination ?? GetDefaultValue(destinationType);
                return config.TypeConverter.DynamicInvoke(source, destinationValue);
            }

            return config.TypeConverter.DynamicInvoke(source);
        }

        if (source == null)
        {
            return existingDestination;
        }

        object destinationForContext = existingDestination ?? CreateDestination(config, source);
        ResolutionContext context = new(source, destinationForContext, sourceType, destinationType, this);

        if (config.PreCondition != null && !InvokeBoolean(config.PreCondition, source, destinationForContext, context))
        {
            return existingDestination;
        }

        if (config.GlobalCondition != null && !InvokeBoolean(config.GlobalCondition, source, destinationForContext, context))
        {
            return existingDestination;
        }

        object destination = destinationForContext;

        foreach (IMappingActionWrapper beforeAction in config.BeforeMapActions)
        {
            beforeAction.Execute(source, destination, context);
        }

        foreach ((Type baseSource, Type baseDestination) in config.BaseTypeMappings)
        {
            MappingConfiguration? baseConfig = _registry.Get(baseSource, baseDestination);
            if (baseConfig != null)
            {
                ApplyBulkMemberOptions(baseConfig);
                MapProperties(source, destination, baseConfig, baseSource, baseDestination, 0, context);
            }
        }

        MapProperties(source, destination, config, sourceType, destinationType, 0, context);

        foreach (IMappingActionWrapper afterAction in config.AfterMapActions)
        {
            afterAction.Execute(source, destination, context);
        }

        return destination;
    }

    private void MapProperties(
        object source,
        object destination,
        MappingConfiguration config,
        Type sourceType,
        Type destinationType,
        int depth,
        ResolutionContext context)
    {
        if (depth > config.MaxDepth)
        {
            return;
        }

        Dictionary<string, PropertyInfo> sourceProperties = sourceType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead)
            .ToDictionary(property => property.Name);

        List<PropertyInfo> destinationProperties = GetWritableProperties(destinationType).ToList();

        if (config.MemberMappingOrder.Count > 0)
        {
            destinationProperties = destinationProperties
                .OrderBy(property => config.MemberMappingOrder.TryGetValue(property.Name, out int order) ? order : int.MaxValue)
                .ToList();
        }

        foreach (PropertyInfo destinationProperty in destinationProperties)
        {
            if (config.IgnoredMembers.Contains(destinationProperty.Name) || IsGloballyIgnored(destinationProperty.Name))
            {
                continue;
            }

            if (config.UseDestinationValueMembers.Contains(destinationProperty.Name))
            {
                continue;
            }

            if (config.MemberPreConditions.TryGetValue(destinationProperty.Name, out Delegate? memberPreCondition)
                && !InvokeBoolean(memberPreCondition, source, destination, context))
            {
                continue;
            }

            object? value = null;
            bool resolved = false;
            Type? sourceMemberType = null;

            if (config.MemberConstants.TryGetValue(destinationProperty.Name, out object? constant))
            {
                value = constant;
                resolved = true;
                sourceMemberType = constant?.GetType();
            }
            else if (config.ValueResolvers.TryGetValue(destinationProperty.Name, out object? valueResolver))
            {
                MethodInfo? resolveMethod = valueResolver.GetType().GetMethod("Resolve");
                object? currentValue = destinationProperty.GetValue(destination);

                value = resolveMethod!.Invoke(valueResolver, [source, destination, currentValue, context]);
                resolved = true;
                sourceMemberType = value?.GetType();
            }
            else if (config.MemberResolvers.TryGetValue(destinationProperty.Name, out Delegate? resolver))
            {
                value = InvokeResolver(resolver, source, destination);
                resolved = true;
                sourceMemberType = value?.GetType();
            }
            else if (sourceProperties.TryGetValue(destinationProperty.Name, out PropertyInfo? sourceProperty))
            {
                value = sourceProperty.GetValue(source);
                resolved = true;
                sourceMemberType = sourceProperty.PropertyType;

                if (config.DeepMapEnabled
                    && value != null
                    && !IsSimpleType(destinationProperty.PropertyType)
                    && _registry.Has(sourceProperty.PropertyType, destinationProperty.PropertyType))
                {
                    MappingConfiguration nestedConfig = _registry.GetOrThrow(sourceProperty.PropertyType, destinationProperty.PropertyType);

                    value = ExecuteMapping(
                        value,
                        null,
                        nestedConfig,
                        sourceProperty.PropertyType,
                        destinationProperty.PropertyType);
                }
            }
            else if (config.FlattenEnabled)
            {
                value = TryGetFlattenedValue(source, sourceProperties, destinationProperty.Name);
                resolved = value != null;
                sourceMemberType = value?.GetType();
            }

            if (!resolved)
            {
                continue;
            }

            object? currentDestinationValue = destinationProperty.GetValue(destination);

            if (config.MemberConditions.TryGetValue(destinationProperty.Name, out Delegate? memberCondition)
                && !InvokeBoolean(memberCondition, source, destination, value, currentDestinationValue, context))
            {
                continue;
            }

            if (value == null && config.MemberNullSubstitutes.TryGetValue(destinationProperty.Name, out object? nullSubstitute))
            {
                value = nullSubstitute;
                sourceMemberType = nullSubstitute?.GetType();
            }

            if (value == null && destinationProperty.PropertyType.IsValueType && Nullable.GetUnderlyingType(destinationProperty.PropertyType) == null)
            {
                continue;
            }

            if (value != null && _registry.GlobalValueTransforms.TryGetValue(destinationProperty.PropertyType, out Delegate? transform))
            {
                value = transform.DynamicInvoke(value);
                sourceMemberType = value?.GetType();
            }

            if (value != null && IsCollectionType(destinationProperty.PropertyType) && IsCollectionType(value.GetType()))
            {
                value = MapCollection(value, destinationProperty.PropertyType, depth + 1);
                sourceMemberType = value?.GetType();
            }

            if (value != null && !destinationProperty.PropertyType.IsAssignableFrom(value.GetType()))
            {
                Type actualSourceMemberType = value.GetType();
                Type destinationMemberType = destinationProperty.PropertyType;

                if (!IsSimpleType(actualSourceMemberType)
                    && !IsSimpleType(destinationMemberType)
                    && _registry.Has(actualSourceMemberType, destinationMemberType))
                {
                    MappingConfiguration nestedConfig = _registry.GetOrThrow(actualSourceMemberType, destinationMemberType);

                    value = ExecuteMapping(
                        value,
                        null,
                        nestedConfig,
                        actualSourceMemberType,
                        destinationMemberType);
                }
                else
                {
                    throw new MappingException(
                        $"No mapping found for '{actualSourceMemberType.Name}' to '{destinationMemberType.Name}' on property '{destinationProperty.Name}'.");
                }
            }

            try
            {
                destinationProperty.SetValue(destination, value);
            }
            catch (Exception exception)
            {
                throw new MappingException($"Error setting '{destinationProperty.Name}' on '{destinationType.Name}': {exception.Message}", exception);
            }
        }
        foreach (KeyValuePair<string, Delegate> pathMapping in config.PathResolvers)
        {
            SetNestedProperty(destination, pathMapping.Key, pathMapping.Value.DynamicInvoke(source));
        }

        foreach (KeyValuePair<string, object?> pathConstant in config.PathConstants)
        {
            SetNestedProperty(destination, pathConstant.Key, pathConstant.Value);
        }
    }

    private void ApplyBulkMemberOptions(MappingConfiguration config)
    {
        _appliedBulkMemberOptions.GetOrAdd(config, currentConfig =>
        {
            HashSet<string> configuredMembers = new(
                currentConfig.MemberResolvers.Keys
                    .Concat(currentConfig.MemberConstants.Keys)
                    .Concat(currentConfig.IgnoredMembers)
                    .Concat(currentConfig.ValueResolvers.Keys)
                    .Concat(currentConfig.PathResolvers.Keys.Select(path => path.Split('.')[0])));

            foreach (PropertyInfo destinationProperty in GetWritableProperties(currentConfig.DestinationType))
            {
                if (currentConfig.ForAllMembersAction != null)
                {
                    ApplyMemberOptionsAction(currentConfig.ForAllMembersAction, destinationProperty.Name, currentConfig);
                }

                if (currentConfig.ForAllOtherMembersAction != null && !configuredMembers.Contains(destinationProperty.Name))
                {
                    ApplyMemberOptionsAction(currentConfig.ForAllOtherMembersAction, destinationProperty.Name, currentConfig);
                }
            }

            return true;
        });
    }

    private static void ApplyMemberOptionsAction(Delegate action, string memberName, MappingConfiguration config)
    {
        Type memberOptionsType = typeof(MemberOptions<,,>).MakeGenericType(config.SourceType, config.DestinationType, typeof(object));
        object memberOptions = Activator.CreateInstance(memberOptionsType, memberName, config)!;
        action.DynamicInvoke(memberOptions);
    }

    private static object? InvokeResolver(Delegate resolver, object source, object destination)
    {
        ParameterInfo[] parameters = resolver.Method.GetParameters();
        return parameters.Length switch
        {
            1 => resolver.DynamicInvoke(source),
            2 => resolver.DynamicInvoke(source, destination),
            _ => throw new MappingException($"Unsupported resolver signature with {parameters.Length} parameters.")
        };
    }

    private static bool InvokeBoolean(Delegate predicate, object source, object destination, ResolutionContext context)
    {
        ParameterInfo[] parameters = predicate.Method.GetParameters();
        object? result = parameters.Length switch
        {
            1 when parameters[0].ParameterType == typeof(ResolutionContext) => predicate.DynamicInvoke(context),
            1 => predicate.DynamicInvoke(source),
            2 => predicate.DynamicInvoke(source, destination),
            3 => predicate.DynamicInvoke(source, destination, context),
            _ => throw new MappingException($"Unsupported predicate signature with {parameters.Length} parameters.")
        };

        return result is not false;
    }

    private static bool InvokeBoolean(
        Delegate predicate,
        object source,
        object destination,
        object? sourceMember,
        object? destinationMember,
        ResolutionContext context)
    {
        ParameterInfo[] parameters = predicate.Method.GetParameters();
        object? result = parameters.Length switch
        {
            1 when parameters[0].ParameterType == typeof(ResolutionContext) => predicate.DynamicInvoke(context),
            1 => predicate.DynamicInvoke(source),
            2 => predicate.DynamicInvoke(source, destination),
            3 => predicate.DynamicInvoke(source, destination, context),
            4 => predicate.DynamicInvoke(source, destination, sourceMember, destinationMember),
            5 => predicate.DynamicInvoke(source, destination, sourceMember, destinationMember, context),
            _ => throw new MappingException($"Unsupported predicate signature with {parameters.Length} parameters.")
        };

        return result is not false;
    }

    private static void SetNestedProperty(object instance, string path, object? value)
    {
        string[] parts = path.Split('.');
        object current = instance;

        for (int index = 0; index < parts.Length - 1; index++)
        {
            PropertyInfo? property = current.GetType().GetProperty(parts[index]);
            if (property == null)
            {
                return;
            }

            object? nextValue = property.GetValue(current);
            if (nextValue == null)
            {
                nextValue = Activator.CreateInstance(property.PropertyType);
                property.SetValue(current, nextValue);
            }

            current = nextValue!;
        }

        PropertyInfo? finalProperty = current.GetType().GetProperty(parts[^1]);
        finalProperty?.SetValue(current, value);
    }

    private static object? TryGetFlattenedValue(
        object source,
        Dictionary<string, PropertyInfo> sourceProperties,
        string destinationMemberName)
    {
        foreach (PropertyInfo property in sourceProperties.Values)
        {
            if (!destinationMemberName.StartsWith(property.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string remainingName = destinationMemberName[property.Name.Length..];
            if (string.IsNullOrEmpty(remainingName))
            {
                return property.GetValue(source);
            }

            object? nestedValue = property.GetValue(source);
            if (nestedValue == null)
            {
                return null;
            }

            Dictionary<string, PropertyInfo> nestedProperties = nestedValue.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(nestedProperty => nestedProperty.CanRead)
                .ToDictionary(nestedProperty => nestedProperty.Name);

            return TryGetFlattenedValue(nestedValue, nestedProperties, remainingName);
        }

        return null;
    }

    private object? MapCollection(object sourceCollection, Type destinationCollectionType, int depth)
    {
        List<object> sourceItems = ((System.Collections.IEnumerable)sourceCollection).Cast<object>().ToList();
        if (sourceItems.Count == 0)
        {
            return CreateEmptyCollection(destinationCollectionType);
        }

        Type? destinationElementType = GetCollectionElementType(destinationCollectionType);
        Type sourceElementType = sourceItems[0].GetType();

        if (destinationElementType == null)
        {
            return sourceCollection;
        }

        MappingConfiguration? elementConfig = _registry.Get(sourceElementType, destinationElementType);

        List<object?> mappedItems = sourceItems
            .Select(item => elementConfig != null
                ? ExecuteMapping(item, null, elementConfig, sourceElementType, destinationElementType)
                : item)
            .ToList();

        return CreateCollection(mappedItems, destinationCollectionType, destinationElementType);
    }

    private static object CreateDestination(MappingConfiguration config, object source)
    {
        if (config.CustomConstructor != null)
        {
            return config.CustomConstructor.DynamicInvoke(source)!;
        }

        try
        {
            return Activator.CreateInstance(config.DestinationType)
                ?? throw new MappingException($"Cannot create instance of '{config.DestinationType.Name}'. Ensure it has a parameterless constructor or use ConstructUsing().");
        }
        catch (MissingMethodException exception)
        {
            throw new MappingException($"'{config.DestinationType.Name}' has no parameterless constructor. Use ConstructUsing() or ensure a public parameterless constructor exists.", exception);
        }
    }

    private static IEnumerable<PropertyInfo> GetWritableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite);
    }

    private static bool TryResolveFlattenedMember(Type sourceType, string destinationMemberName)
    {
        foreach (PropertyInfo property in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!destinationMemberName.StartsWith(property.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string remainingName = destinationMemberName[property.Name.Length..];
            if (string.IsNullOrEmpty(remainingName))
            {
                return true;
            }

            if (TryResolveFlattenedMember(property.PropertyType, remainingName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(Guid)
            || type == typeof(TimeSpan)
            || type.IsEnum
            || Nullable.GetUnderlyingType(type) != null;
    }

    private static bool IsCollectionType(Type type)
    {
        return type != typeof(string)
            && (type.IsArray || type.GetInterfaces().Any(interfaceType =>
                interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    private static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        return collectionType.GetInterfaces()
            .FirstOrDefault(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static object CreateEmptyCollection(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return Array.CreateInstance(collectionType.GetElementType()!, 0);
        }

        Type? elementType = GetCollectionElementType(collectionType);
        if (elementType == null)
        {
            return new List<object>();
        }

        return Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
    }

    private static object CreateCollection(List<object?> items, Type collectionType, Type elementType)
    {
        if (collectionType.IsArray)
        {
            Array array = Array.CreateInstance(elementType, items.Count);
            for (int index = 0; index < items.Count; index++)
            {
                array.SetValue(items[index], index);
            }

            return array;
        }

        Type concreteCollectionType = collectionType.IsInterface || collectionType.IsAbstract
            ? typeof(List<>).MakeGenericType(elementType)
            : collectionType;

        System.Collections.IList list = (System.Collections.IList)Activator.CreateInstance(concreteCollectionType)!;
        foreach (object? item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private bool IsGloballyIgnored(string memberName)
    {
        return _registry.GlobalIgnorePredicates.Any(predicate => predicate(memberName));
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
