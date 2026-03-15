namespace FlowR.Mapper;

/// <summary>
/// Thrown when no mapping is registered between two types.
/// </summary>
public sealed class MappingNotFoundException : Exception
{
    public Type SourceType { get; }
    public Type DestinationType { get; }

    public MappingNotFoundException(Type source, Type dest)
        : base($"No mapping found from '{source.FullName}' to '{dest.FullName}'. " +
               $"Did you call CreateMap<{source.Name}, {dest.Name}>() in a MapperProfile?")
    {
        SourceType = source;
        DestinationType = dest;
    }
}

/// <summary>
/// Thrown when a mapping fails at runtime.
/// </summary>
public sealed class MappingException : Exception
{
    public MappingException(string message) : base(message) { }
    public MappingException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Thrown when mapper configuration is invalid (missing mappings, unmapped members, etc.)
/// </summary>
public sealed class MapperConfigurationException : Exception
{
    public MapperConfigurationException(string message) : base(message) { }
}
