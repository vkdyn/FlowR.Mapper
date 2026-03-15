using System.Linq.Expressions;

namespace FlowR.Mapper;

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
    /// Called after the mapping runs.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMap(
        Action<TSource, TDestination> action);

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
}

/// <summary>
/// Options for configuring an individual destination member.
/// </summary>
public interface IMemberOptions<TSource, TDestination, TMember>
{
    /// <summary>Maps from a source expression.</summary>
    IMemberOptions<TSource, TDestination, TMember> MapFrom<TSourceMember>(
        Expression<Func<TSource, TSourceMember>> sourceMember);

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
