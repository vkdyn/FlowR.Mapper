using FlowR.Mapper.Interfaces;

namespace FlowR.Mapper.Core;

/// <summary>
/// Provides context and state during a mapping operation.
/// Similar to AutoMapper's ResolutionContext.
/// </summary>
public class ResolutionContext
{
    /// <summary>
    /// The source object being mapped.
    /// </summary>
    public object Source { get; }

    /// <summary>
    /// The destination object being mapped to.
    /// </summary>
    public object? Destination { get; }

    /// <summary>
    /// The source type.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// The destination type.
    /// </summary>
    public Type DestinationType { get; }

    /// <summary>
    /// Custom items that can be passed through the mapping pipeline.
    /// </summary>
    public IDictionary<string, object> Items { get; }

    /// <summary>
    /// The current mapping depth (for circular reference protection).
    /// </summary>
    public int Depth { get; internal set; }

    /// <summary>
    /// Reference to the mapper instance.
    /// </summary>
    public IMapper Mapper { get; }

    internal ResolutionContext(
        object source,
        object? destination,
        Type sourceType,
        Type destinationType,
        IMapper mapper,
        IDictionary<string, object>? items = null)
    {
        Source = source;
        Destination = destination;
        SourceType = sourceType;
        DestinationType = destinationType;
        Mapper = mapper;
        Items = items ?? new Dictionary<string, object>();
        Depth = 0;
    }

    /// <summary>
    /// Maps a source object to a destination type using the context's mapper.
    /// </summary>
    public TDest Map<TSource, TDest>(TSource source)
    {
        return Mapper.Map<TSource, TDest>(source);
    }

    /// <summary>
    /// Maps a source object to an existing destination instance.
    /// </summary>
    public TDest Map<TSource, TDest>(TSource source, TDest destination)
    {
        return Mapper.Map(source, destination);
    }
}
