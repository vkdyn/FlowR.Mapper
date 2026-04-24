namespace FlowR.Mapper;

using FlowR.Mapper;
using System.Linq;

/// <summary>
/// Provides IQueryable projection extension methods for FlowR.Mapper.
/// </summary>
public static class QueryableMappingExtensions
{
    /// <summary>
    /// Projects an IQueryable source into an IQueryable destination using FlowR.Mapper.
    /// </summary>
    /// <typeparam name="TSource">Source entity type.</typeparam>
    /// <typeparam name="TDestination">Destination DTO type.</typeparam>
    /// <param name="source">Source query.</param>
    /// <param name="mapper">FlowR mapper.</param>
    /// <returns>Projected query.</returns>
    public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
        this IQueryable<TSource> source,
        IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mapper);

        return mapper.ProjectTo<TSource, TDestination>(source);
    }
}