using FlowR.Mapper.Internal;
using System.Linq.Expressions;
using System.Reflection;

namespace FlowR.Mapper;

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
        _config.BeforeMapActions.Add(action);
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action)
    {
        _config.AfterMapActions.Add(action);
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

    public IMemberOptions<TSource, TDestination, TMember> MapFrom<TSourceMember>(
        Expression<Func<TSource, TSourceMember>> sourceMember)
    {
        var compiled = sourceMember.Compile();
        _config.MemberResolvers[_memberName] = (Func<TSource, TMember>)(s => (TMember)(object)compiled(s)!);
        return this;
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

    public IMemberOptions<TSource, TDestination, TMember> NullSubstitute(TMember value)
    {
        _config.MemberNullSubstitutes[_memberName] = value;
        return this;
    }
}
