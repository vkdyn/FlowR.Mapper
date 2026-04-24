using FlowR.Mapper;
using FlowR.Mapper.Core;
using FlowR.Mapper.Interfaces;
using FlowR.Mapper.Internal;
using System.Linq.Expressions;
using System.Reflection;

namespace FlowR.Mapper.Configuration;

internal sealed class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
{
    private readonly MappingConfiguration _config;
    private readonly MapperRegistry _registry;

    public MappingExpression(MappingConfiguration config, MapperRegistry registry)
    {
        _config = config;
        _registry = registry;
    }

    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberOptions<TSource, TDestination, TMember>> options)
    {
        var memberName = GetMemberName(destinationMember);
        var memberOptions = new MemberOptions<TSource, TDestination, TMember>(memberName, _config);
        options(memberOptions);
        return this;
    }

    public IMappingExpression<TSource, TDestination> Ignore<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember)
    {
        _config.IgnoredMembers.Add(GetMemberName(destinationMember));
        return this;
    }

    public IMappingExpression<TSource, TDestination> When(Func<TSource, bool> condition)
    {
        _config.GlobalCondition = condition;
        return this;
    }

    public IMappingExpression<TSource, TDestination> When(Func<TSource, TDestination, bool> condition)
    {
        _config.GlobalCondition = condition;
        return this;
    }

    public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action)
    {
        _config.BeforeMapActions.Add(new MappingActionWrapper<TSource, TDestination>(action));
        return this;
    }

    public IMappingExpression<TSource, TDestination> BeforeMap(IMappingAction<TSource, TDestination> action)
    {
        _config.BeforeMapActions.Add(new MappingActionWithContextWrapper<TSource, TDestination>(
            (src, dest, ctx) => action.Process(src, dest, ctx)));
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action)
    {
        _config.AfterMapActions.Add(new MappingActionWrapper<TSource, TDestination>(action));
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> action)
    {
        _config.AfterMapActions.Add(new MappingActionWithContextWrapper<TSource, TDestination>(action));
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(IMappingAction<TSource, TDestination> action)
    {
        _config.AfterMapActions.Add(new MappingActionWithContextWrapper<TSource, TDestination>(
            (src, dest, ctx) => action.Process(src, dest, ctx)));
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> constructor)
    {
        _config.CustomConstructor = constructor;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForCollection(Action<ICollectionOptions> options)
    {
        // Collection options stored in config for use during compilation
        return this;
    }

    public IMappingExpression<TSource, TDestination> DeepMap()
    {
        _config.DeepMapEnabled = true;
        return this;
    }

    public IMappingExpression<TSource, TDestination> Flatten()
    {
        _config.FlattenEnabled = true;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ReverseMap()
    {
        _config.ReverseMapEnabled = true;
        // Register reverse mapping automatically
        var reverseConfig = new MappingConfiguration(typeof(TDestination), typeof(TSource));
        _registry.Register(reverseConfig);
        return this;
    }

    public IMappingExpression<TSource, TDestination> ValidateAllMembersAreMapped()
    {
        _config.ValidateAllMembers = true;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(ITypeConverter<TSource, TDestination> converter)
    {
        _config.TypeConverter = (Func<TSource, TDestination>)(s => converter.Convert(s));
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter)
    {
        _config.TypeConverter = converter;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination, TDestination> converter)
    {
        _config.TypeConverter = converter;
        return this;
    }

    public IMappingExpression<TSource, TDestination> Include<TDerivedSource, TDerivedDestination>()
        where TDerivedSource : TSource
        where TDerivedDestination : TDestination
    {
        _config.DerivedTypeMappings.Add((typeof(TDerivedSource), typeof(TDerivedDestination)));
        return this;
    }

    public IMappingExpression<TSource, TDestination> NullSubstitute(TDestination substitute)
    {
        _config.NullSubstitute = substitute;
        return this;
    }

    public IMappingExpression<TSource, TDestination> MaxDepth(int depth)
    {
        _config.MaxDepth = depth;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberOptions<TSource, TDestination, object>> memberOptions)
    {
        _config.ForAllMembersAction = memberOptions;
        return this;
    }

    public IMappingExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDestination>()
        where TBaseSource : class
        where TBaseDestination : class
    {
        _config.BaseTypeMappings.Add((typeof(TBaseSource), typeof(TBaseDestination)));
        return this;
    }

    public IMappingExpression<TSource, TDestination> PreCondition(Func<TSource, bool> condition)
    {
        _config.PreCondition = condition;
        return this;
    }

    public IMappingExpression<TSource, TDestination> PreCondition(Func<TSource, TDestination, bool> condition)
    {
        _config.PreCondition = condition;
        return this;
    }

    public IMappingExpression<TSource, TDestination> PreCondition(Func<ResolutionContext, bool> condition)
    {
        _config.PreCondition = condition;
        return this;
    }

    public IMappingExpression<TSource, TDestination> AllowNull()
    {
        _config.AllowNullSource = true;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationPath,
        Action<IPathOptions<TSource, TDestination, TMember>> options)
    {
        var pathString = GetPropertyPath(destinationPath);
        var pathOptions = new PathOptions<TSource, TDestination, TMember>(pathString, _config);
        options(pathOptions);
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForAllOtherMembers(
        Action<IMemberOptions<TSource, TDestination, object>> memberOptions)
    {
        _config.ForAllOtherMembersAction = memberOptions;
        return this;
    }

    public IMappingExpression<TSource, TDestination> SetMappingOrder(
        Expression<Func<TDestination, object>> destinationMember,
        int order)
    {
        var memberName = GetMemberName(destinationMember);
        _config.MemberMappingOrder[memberName] = order;
        return this;
    }

    private static string GetPropertyPath<T, TMember>(Expression<Func<T, TMember>> expression)
    {
        var parts = new List<string>();
        var current = expression.Body;
        
        while (current is MemberExpression member)
        {
            parts.Insert(0, member.Member.Name);
            current = member.Expression;
        }
        
        return string.Join(".", parts);
    }

    private static string GetMemberName<T, TMember>(Expression<Func<T, TMember>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException($"Expression '{expression}' is not a member access expression.");
    }
}

internal sealed class MemberOptions<TSource, TDestination, TMember>
    : IMemberOptions<TSource, TDestination, TMember>
{
    private readonly string _memberName;
    private readonly MappingConfiguration _config;

    public MemberOptions(string memberName, MappingConfiguration config)
    {
        _memberName = memberName;
        _config = config;
    }

    public IMemberOptions<TSource, TDestination, TMember> MapFrom(Func<TSource, TMember> resolver)
    {
        _config.MemberResolvers[_memberName] = resolver;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> MapFrom(Func<TSource, TDestination, TMember> resolver)
    {
        _config.MemberResolvers[_memberName] = resolver;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> UseValue(TMember value)
    {
        _config.MemberConstants[_memberName] = value;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> Ignore()
    {
        _config.IgnoredMembers.Add(_memberName);
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> Condition(Func<TSource, bool> condition)
    {
        _config.MemberConditions[_memberName] = condition;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> Condition(Func<TSource, TDestination, bool> condition)
    {
        _config.MemberConditions[_memberName] = condition;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> Condition(Func<TSource, TDestination, TMember?, TMember?, bool> condition)
    {
        _config.MemberConditions[_memberName] = condition;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> PreCondition(Func<TSource, bool> condition)
    {
        _config.MemberPreConditions[_memberName] = condition;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> PreCondition(Func<TSource, TDestination, bool> condition)
    {
        _config.MemberPreConditions[_memberName] = condition;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> PreCondition(Func<TSource, TDestination, ResolutionContext, bool> condition)
    {
        _config.MemberPreConditions[_memberName] = condition;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> AllowNull()
    {
        _config.AllowNullMembers.Add(_memberName);
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> NullSubstitute(TMember value)
    {
        _config.MemberNullSubstitutes[_memberName] = value;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> MapFrom<TResolver>()
        where TResolver : IValueResolver<TSource, TDestination, TMember>, new()
    {
        var resolver = new TResolver();
        return MapFrom(resolver);
    }

    public IMemberOptions<TSource, TDestination, TMember> MapFrom<TResolver>(TResolver resolver)
        where TResolver : IValueResolver<TSource, TDestination, TMember>
    {
        _config.ValueResolvers[_memberName] = resolver;
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> UseDestinationValue()
    {
        _config.UseDestinationValueMembers.Add(_memberName);
        return this;
    }

    public IMemberOptions<TSource, TDestination, TMember> SetMappingOrder(int order)
    {
        _config.MemberMappingOrder[_memberName] = order;
        return this;
    }
}

// PathOptions implementation
internal sealed class PathOptions<TSource, TDestination, TMember>
    : IPathOptions<TSource, TDestination, TMember>
{
    private readonly string _path;
    private readonly MappingConfiguration _config;

    public PathOptions(string path, MappingConfiguration config)
    {
        _path = path;
        _config = config;
    }

    public IPathOptions<TSource, TDestination, TMember> MapFrom(Func<TSource, TMember> resolver)
    {
        _config.PathResolvers[_path] = resolver;
        return this;
    }

    public IPathOptions<TSource, TDestination, TMember> UseValue(TMember value)
    {
        _config.PathConstants[_path] = value;
        return this;
    }
}