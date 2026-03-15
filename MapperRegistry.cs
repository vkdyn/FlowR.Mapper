using System.Collections.Concurrent;
using System.Reflection;

namespace FlowR.Mapper.Internal;

/// <summary>
/// Central registry that holds all mapping configurations.
/// Compiles mapping functions on first use and caches them.
/// </summary>
internal sealed class MapperRegistry
{
    // Key: (TSource, TDestination) -> MappingConfiguration
    private readonly ConcurrentDictionary<(Type, Type), MappingConfiguration> _configs = new();
    // Global value transforms: type -> transform func
    internal readonly Dictionary<Type, Delegate> GlobalValueTransforms = new();
    // Global ignored member name predicates
    internal readonly List<Func<string, bool>> GlobalIgnorePredicates = [];

    public void Register(MappingConfiguration config)
    {
        _configs[(config.SourceType, config.DestinationType)] = config;
    }

    public MappingConfiguration? Get(Type source, Type dest)
        => _configs.TryGetValue((source, dest), out var cfg) ? cfg : null;

    public bool Has(Type source, Type dest)
        => _configs.ContainsKey((source, dest));

    public IEnumerable<MappingConfiguration> All() => _configs.Values;

    public MappingConfiguration GetOrThrow(Type source, Type dest)
    {
        var config = Get(source, dest);
        if (config == null)
            throw new MappingNotFoundException(source, dest);
        return config;
    }
}
