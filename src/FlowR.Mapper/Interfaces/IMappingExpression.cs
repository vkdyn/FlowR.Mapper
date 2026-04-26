using FlowR.Mapper.Configuration;
using FlowR.Mapper.Core;
using System.Linq.Expressions;

namespace FlowR.Mapper;

/// <summary>
/// Fluent configuration for a TSource -> TDestination mapping.
/// </summary>
public interface IMappingExpression<TSource, TDestination>
{
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberOptions<TSource, TDestination, TMember>> options);

    IMappingExpression<TSource, TDestination> Ignore<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember);

    IMappingExpression<TSource, TDestination> When(Func<TSource, bool> condition);
    IMappingExpression<TSource, TDestination> When(Func<TSource, TDestination, bool> condition);

    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action);
    IMappingExpression<TSource, TDestination> BeforeMap(IMappingAction<TSource, TDestination> action);

    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action);
    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> action);
    IMappingExpression<TSource, TDestination> AfterMap(IMappingAction<TSource, TDestination> action);

    IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> constructor);

    IMappingExpression<TSource, TDestination> ForCollection(Action<ICollectionOptions> options);

    IMappingExpression<TSource, TDestination> DeepMap();
    IMappingExpression<TSource, TDestination> Flatten();
    IMappingExpression<TSource, TDestination> ReverseMap();

    IMappingExpression<TSource, TDestination> ValidateAllMembersAreMapped();

    IMappingExpression<TSource, TDestination> ConvertUsing(ITypeConverter<TSource, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination, TDestination> converter);

    IMappingExpression<TSource, TDestination> Include<TDerivedSource, TDerivedDestination>()
        where TDerivedSource : TSource
        where TDerivedDestination : TDestination;

    IMappingExpression<TSource, TDestination> NullSubstitute(TDestination substitute);
    IMappingExpression<TSource, TDestination> MaxDepth(int depth);

    IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberOptions<TSource, TDestination, object>> memberOptions);

    IMappingExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDestination>()
        where TBaseSource : class
        where TBaseDestination : class;

    IMappingExpression<TSource, TDestination> PreCondition(Func<TSource, bool> condition);
    IMappingExpression<TSource, TDestination> PreCondition(Func<TSource, TDestination, bool> condition);
    IMappingExpression<TSource, TDestination> PreCondition(Func<ResolutionContext, bool> condition);

    IMappingExpression<TSource, TDestination> AllowNull();

    IMappingExpression<TSource, TDestination> ForPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationPath,
        Action<IPathOptions<TSource, TDestination, TMember>> options);

    IMappingExpression<TSource, TDestination> ForAllOtherMembers(
        Action<IMemberOptions<TSource, TDestination, object>> memberOptions);

    IMappingExpression<TSource, TDestination> SetMappingOrder(
        Expression<Func<TDestination, object>> destinationMember,
        int order);
}

/// <summary>
/// Member-level mapping options.
/// </summary>
public interface IMemberOptions<TSource, TDestination, TMember>
{
    /// <summary>Maps from a compiled resolver function (runtime mapping only).</summary>
    IMemberOptions<TSource, TDestination, TMember> MapFrom(Func<TSource, TMember> resolver);

    /// <summary>
    /// Maps from a LINQ expression tree. Preferred over MapFrom() when the mapping is used
    /// with ProjectTo() — EF Core can translate expression trees directly to SQL.
    /// </summary>
    IMemberOptions<TSource, TDestination, TMember> MapFromExpression(
        Expression<Func<TSource, TMember>> resolverExpression);

    IMemberOptions<TSource, TDestination, TMember> MapFrom(Func<TSource, TDestination, TMember> resolver);

    IMemberOptions<TSource, TDestination, TMember> UseValue(TMember value);
    IMemberOptions<TSource, TDestination, TMember> Ignore();

    IMemberOptions<TSource, TDestination, TMember> Condition(Func<TSource, bool> condition);
    IMemberOptions<TSource, TDestination, TMember> Condition(Func<TSource, TDestination, bool> condition);
    IMemberOptions<TSource, TDestination, TMember> Condition(Func<TSource, TDestination, TMember?, TMember?, bool> condition);

    IMemberOptions<TSource, TDestination, TMember> PreCondition(Func<TSource, bool> condition);
    IMemberOptions<TSource, TDestination, TMember> PreCondition(Func<TSource, TDestination, bool> condition);
    IMemberOptions<TSource, TDestination, TMember> PreCondition(Func<TSource, TDestination, ResolutionContext, bool> condition);

    IMemberOptions<TSource, TDestination, TMember> AllowNull();
    IMemberOptions<TSource, TDestination, TMember> NullSubstitute(TMember value);

    IMemberOptions<TSource, TDestination, TMember> MapFrom<TResolver>()
        where TResolver : IValueResolver<TSource, TDestination, TMember>, new();

    IMemberOptions<TSource, TDestination, TMember> MapFrom<TResolver>(TResolver resolver)
        where TResolver : IValueResolver<TSource, TDestination, TMember>;

    IMemberOptions<TSource, TDestination, TMember> UseDestinationValue();
    IMemberOptions<TSource, TDestination, TMember> SetMappingOrder(int order);
}

/// <summary>
/// Options for collection stub — reserved for future use.
/// </summary>
public interface ICollectionOptions
{
    ICollectionOptions PreserveOrder(bool preserve);
    ICollectionOptions UseEquality<TKey>(Func<object, TKey> keySelector);
}

/// <summary>
/// Path-based nested property options.
/// </summary>
public interface IPathOptions<TSource, TDestination, TMember>
{
    IPathOptions<TSource, TDestination, TMember> MapFrom(Func<TSource, TMember> resolver);
    IPathOptions<TSource, TDestination, TMember> UseValue(TMember value);
}