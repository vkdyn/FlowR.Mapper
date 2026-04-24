using FlowR.Mapper.Core;

namespace FlowR.Mapper;

/// <summary>
/// Defines a custom resolver for mapping a specific destination member.
/// Allows creating reusable, testable resolver classes.
/// </summary>
/// <typeparam name="TSource">Source type</typeparam>
/// <typeparam name="TDestination">Destination type</typeparam>
/// <typeparam name="TDestMember">Type of the destination member</typeparam>
public interface IValueResolver<in TSource, in TDestination, TDestMember>
{
    /// <summary>
    /// Implements custom resolution logic for a destination member.
    /// </summary>
    /// <param name="source">Source object</param>
    /// <param name="destination">Destination object</param>
    /// <param name="destMember">Current destination member value</param>
    /// <param name="context">Resolution context</param>
    /// <returns>The resolved value for the destination member</returns>
    TDestMember Resolve(TSource source, TDestination destination, TDestMember destMember, ResolutionContext context);
}
