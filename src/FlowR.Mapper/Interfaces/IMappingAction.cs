using FlowR.Mapper.Core;

namespace FlowR.Mapper.Interfaces;

/// <summary>
/// Defines a custom action that can be executed during mapping.
/// This is the interface-based alternative to lambda-based AfterMap/BeforeMap.
/// </summary>
public interface IMappingAction<in TSource, in TDestination>
{
    /// <summary>
    /// Performs custom logic during the mapping process.
    /// </summary>
    /// <param name="source">The source object.</param>
    /// <param name="destination">The destination object.</param>
    /// <param name="context">The resolution context.</param>
    void Process(TSource source, TDestination destination, ResolutionContext context);
}
