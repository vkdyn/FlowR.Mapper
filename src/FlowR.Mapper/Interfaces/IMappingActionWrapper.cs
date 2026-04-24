using FlowR.Mapper;
using FlowR.Mapper.Internal;

namespace FlowR.Mapper.Interfaces;

/// <summary>
/// Wraps Action<TSource, TDestination> (2 parameters - no context)
/// </summary>
internal class MappingActionWrapper<TSource, TDestination> : IMappingActionWrapper
{
    private readonly Action<TSource, TDestination> _action;

    public MappingActionWrapper(Action<TSource, TDestination> action)
    {
        _action = action;
    }

    public void Execute(object source, object destination, ResolutionContext? context)
    {
        _action((TSource)source, (TDestination)destination);
    }
}

/// <summary>
/// Wraps Action<TSource, TDestination, ResolutionContext> (3 parameters - with context)
/// </summary>
internal class MappingActionWithContextWrapper<TSource, TDestination> : IMappingActionWrapper
{
    private readonly Action<TSource, TDestination, ResolutionContext> _action;

    public MappingActionWithContextWrapper(Action<TSource, TDestination, ResolutionContext> action)
    {
        _action = action;
    }

    public void Execute(object source, object destination, ResolutionContext? context)
    {
        if (context == null)
            throw new InvalidOperationException("ResolutionContext is required for this mapping action");

        _action((TSource)source, (TDestination)destination, context);
    }
}