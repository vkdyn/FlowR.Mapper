using FlowR.Mapper.Configuration;
using FlowR.Mapper.Core;
using System.Linq.Expressions;

namespace FlowR.Mapper.Interfaces;

/// <summary>
/// Fluent API for configuring a mapping between TSource and TDestination.
/// </summary>
public interface IMappingExpression<TSource, TDestination>
{
    /// <summary>
    /// Maps a destination member using a custom resolver.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberOptions<TSource, TDestination, TMember>> options);

    /// <summary>
    /// Ignores a destination member entirely.
    /// </summary>
    IMappingExpression<TSource, TDestination> Ignore<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember);

    /// <summary>
    /// Applies a condition — mapping only executes if the condition returns true.
    /// </summary>
    IMappingExpression<TSource, TDestination> When(
        Func<TSource, bool> condition);

    /// <summary>
    /// Applies a condition based on both source and destination.
    /// </summary>
    IMappingExpression<TSource, TDestination> When(
        Func<TSource, TDestination, bool> condition);

    /// <summary>
    /// Called before the mapping runs.
    /// </summary>
    IMappingExpression<TSource, TDestination> BeforeMap(
        Action<TSource, TDestination> action);

    /// <summary>
    /// Called before the mapping runs using a custom mapping action.
    /// </summary>
    IMappingExpression<TSource, TDestination> BeforeMap(
        IMappingAction<TSource, TDestination> action);

    /// <summary>
    /// Called after the mapping runs.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMap(
        Action<TSource, TDestination> action);

    /// <summary>
    /// Called after the mapping runs with context support.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMap(
        Action<TSource, TDestination, ResolutionContext> action);

    /// <summary>
    /// Called after the mapping runs using a custom mapping action.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMap(
        IMappingAction<TSource, TDestination> action);

    /// <summary>
    /// Maps to the destination using a constructor.
    /// Useful for immutable records/DTOs.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConstructUsing(
        Func<TSource, TDestination> constructor);

    /// <summary>
    /// Configures how collections of the source type are mapped.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForCollection(
        Action<ICollectionOptions> options);

    /// <summary>
    /// Enables deep/nested mapping — FlowR will auto-recurse into complex types.
    /// On by default when nested type mappings are registered.
    /// </summary>
    IMappingExpression<TSource, TDestination> DeepMap();

    /// <summary>
    /// Flattens nested properties using dot notation or naming conventions.
    /// E.g., source.Address.City -> destination.AddressCity
    /// </summary>
    IMappingExpression<TSource, TDestination> Flatten();

    /// <summary>
    /// Reverses the mapping, creating TDestination -> TSource automatically.
    /// </summary>
    IMappingExpression<TSource, TDestination> ReverseMap();

    /// <summary>
    /// Validates this mapping immediately — throws if any destination member is unmapped and not ignored.
    /// </summary>
    IMappingExpression<TSource, TDestination> ValidateAllMembersAreMapped();

    /// <summary>
    /// Registers a type converter for this mapping pair.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConvertUsing(
        ITypeConverter<TSource, TDestination> converter);

    /// <summary>
    /// Registers a custom conversion function for this mapping pair.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConvertUsing(
        Func<TSource, TDestination> converter);

    /// <summary>
    /// Registers a custom conversion function with destination context for this mapping pair.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConvertUsing(
        Func<TSource, TDestination, TDestination> converter);

    /// <summary>
    /// Includes derived type mapping. Useful for polymorphism.
    /// </summary>
    IMappingExpression<TSource, TDestination> Include<TDerivedSource, TDerivedDestination>()
        where TDerivedSource : TSource
        where TDerivedDestination : TDestination;

    /// <summary>
    /// Maps null source values to a specific value instead of null.
    /// </summary>
    IMappingExpression<TSource, TDestination> NullSubstitute(TDestination substitute);

    /// <summary>
    /// Maximum depth for recursive/circular reference mapping. Default: 5.
    /// </summary>
    IMappingExpression<TSource, TDestination> MaxDepth(int depth);

    /// <summary>
    /// Applies configuration to all destination members at once.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberOptions<TSource, TDestination, object>> memberOptions);

    /// <summary>
    /// Includes the base type mapping configuration in this derived mapping.
    /// </summary>
    IMappingExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDestination>()
        where TBaseSource : class
        where TBaseDestination : class;

    /// <summary>
    /// Specifies a precondition that must be satisfied before mapping occurs.
    /// If the precondition fails, the mapping is skipped.
    /// </summary>
    IMappingExpression<TSource, TDestination> PreCondition(
        Func<TSource, bool> condition);

    /// <summary>
    /// Specifies a precondition with destination context.
    /// </summary>
    IMappingExpression<TSource, TDestination> PreCondition(
        Func<TSource, TDestination, bool> condition);

    /// <summary>
    /// Specifies a precondition with full resolution context.
    /// </summary>
    IMappingExpression<TSource, TDestination> PreCondition(
        Func<ResolutionContext, bool> condition);

    /// <summary>
    /// Allows null values to propagate through the mapping.
    /// By default, FlowR substitutes null source with default(TDestination).
    /// </summary>
    IMappingExpression<TSource, TDestination> AllowNull();

    /// <summary>
    /// Maps to a nested destination property path (e.g., d => d.Address.City).
    /// </summary>
    IMappingExpression<TSource, TDestination> ForPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationPath,
        Action<IPathOptions<TSource, TDestination, TMember>> options);

    /// <summary>
    /// Applies configuration to all members that haven't been explicitly configured.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForAllOtherMembers(
        Action<IMemberOptions<TSource, TDestination, object>> memberOptions);

    /// <summary>
    /// Sets the mapping order for a specific member. Lower values map first.
    /// </summary>
    IMappingExpression<TSource, TDestination> SetMappingOrder(
        Expression<Func<TDestination, object>> destinationMember,
        int order);
}

/// <summary>
/// Options for configuring an individual destination member.
/// </summary>
public interface IMemberOptions<TSource, TDestination, TMember>
{
    /// <summary>Maps from a custom resolve function.</summary>
    IMemberOptions<TSource, TDestination, TMember> MapFrom(
        Func<TSource, TMember> resolver);

    /// <summary>Maps from a custom resolve function with context (destination access).</summary>
    IMemberOptions<TSource, TDestination, TMember> MapFrom(
        Func<TSource, TDestination, TMember> resolver);

    /// <summary>Sets a constant value.</summary>
    IMemberOptions<TSource, TDestination, TMember> UseValue(TMember value);

    /// <summary>Ignores this member.</summary>
    IMemberOptions<TSource, TDestination, TMember> Ignore();

    /// <summary>Only maps if condition is true.</summary>
    IMemberOptions<TSource, TDestination, TMember> Condition(Func<TSource, bool> condition);

    /// <summary>Substitute value when source is null.</summary>
    IMemberOptions<TSource, TDestination, TMember> NullSubstitute(TMember value);

    /// <summary>
    /// Uses a custom value resolver class for this member.
    /// </summary>
    IMemberOptions<TSource, TDestination, TMember> MapFrom<TResolver>()
        where TResolver : IValueResolver<TSource, TDestination, TMember>, new();

    /// <summary>
    /// Uses a custom value resolver instance for this member.
    /// </summary>
    IMemberOptions<TSource, TDestination, TMember> MapFrom<TResolver>(TResolver resolver)
        where TResolver : IValueResolver<TSource, TDestination, TMember>;

    /// <summary>
    /// Keeps the existing destination value instead of mapping from source.
    /// </summary>
    IMemberOptions<TSource, TDestination, TMember> UseDestinationValue();

    /// <summary>
    /// Sets the mapping order for this member. Lower values map first.
    /// </summary>
    IMemberOptions<TSource, TDestination, TMember> SetMappingOrder(int order);
}

/// <summary>
/// Options for collection mapping.
/// </summary>
public interface ICollectionOptions
{
    /// <summary>Preserve order of source collection. Default: true.</summary>
    ICollectionOptions PreserveOrder(bool preserve = true);

    /// <summary>Use a specific equality comparer when merging collections.</summary>
    ICollectionOptions UseEquality<TKey>(Func<object, TKey> keySelector);
}

/// <summary>
/// Options for configuring a nested destination property path.
/// </summary>
public interface IPathOptions<TSource, TDestination, TMember>
{
    /// <summary>Maps from a custom resolve function.</summary>
    IPathOptions<TSource, TDestination, TMember> MapFrom(
        Func<TSource, TMember> resolver);

    /// <summary>Sets a constant value.</summary>
    IPathOptions<TSource, TDestination, TMember> UseValue(TMember value);
}
