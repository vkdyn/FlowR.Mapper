// ------------------------------------------------------------------------------------------
//
// AutoMapperCompatibilityExtensions.cs -- AutoMapper-style compatibility helpers.
//
// Copyright (c) 2026 FlowR.Mapper. All rights reserved.
//
// ------------------------------------------------------------------------------------------

namespace FlowR.Mapper;

/// <summary>
/// AutoMapper-style compatibility helpers for FlowR.Mapper.
/// These exist to support migration of large existing AutoMapper profiles.
/// </summary>
public static class AutoMapperCompatibilityExtensions
{
    /// <summary>
    /// Allows null source values for the configured member.
    /// </summary>
    public static IMemberOptions<TSource, TDestination, TMember> AllowNull<TSource, TDestination, TMember>(
        this IMemberOptions<TSource, TDestination, TMember> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.AllowNull();
    }

    /// <summary>
    /// Applies a precondition with source, destination and resolution context.
    /// </summary>
    public static IMemberOptions<TSource, TDestination, TMember> PreCondition<TSource, TDestination, TMember>(
        this IMemberOptions<TSource, TDestination, TMember> options,
        Func<TSource, TDestination, ResolutionContext, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(condition);

        return options.PreCondition(condition);
    }

    /// <summary>
    /// Applies a precondition with source and destination.
    /// </summary>
    public static IMemberOptions<TSource, TDestination, TMember> PreCondition<TSource, TDestination, TMember>(
        this IMemberOptions<TSource, TDestination, TMember> options,
        Func<TSource, TDestination, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(condition);

        return options.PreCondition(condition);
    }

    /// <summary>
    /// Applies an AutoMapper-style member condition using source, destination,
    /// resolved source member and current destination member.
    /// </summary>
    public static IMemberOptions<TSource, TDestination, TMember> Condition<TSource, TDestination, TMember>(
        this IMemberOptions<TSource, TDestination, TMember> options,
        Func<TSource, TDestination, TMember?, TMember?, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(condition);

        return options.Condition(condition);
    }

    /// <summary>
    /// Applies an AutoMapper-style member condition using source, destination,
    /// resolved source member, current destination member and resolution context.
    /// </summary>
    public static IMemberOptions<TSource, TDestination, TMember> Condition<TSource, TDestination, TMember>(
        this IMemberOptions<TSource, TDestination, TMember> options,
        Func<TSource, TDestination, TMember?, TMember?, ResolutionContext, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(condition);

        return options.Condition((source, destination, sourceMember, destinationMember) =>
        {
            ResolutionContext context = new(
                source!,
                destination,
                typeof(TSource),
                typeof(TDestination),
                default!,
                null);

            return condition(source, destination, sourceMember, destinationMember, context);
        });
    }

    /// <summary>
    /// Maps a member from a resolver type.
    /// </summary>
    public static IMemberOptions<TSource, TDestination, TMember> MapFrom<TSource, TDestination, TMember, TResolver>(
        this IMemberOptions<TSource, TDestination, TMember> options)
        where TResolver : IValueResolver<TSource, TDestination, TMember>, new()
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.MapFrom<TResolver>();
    }
}
