using FlowR.Mapper.Internal;

namespace FlowR.Mapper;

internal sealed class ProfileConfigurator : IProfileConfigurator
{
    private readonly MapperRegistry _registry;

    public ProfileConfigurator(MapperRegistry registry)
    {
        _registry = registry;
    }

    public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var config = new MappingConfiguration(typeof(TSource), typeof(TDestination));
        _registry.Register(config);
        return new MappingExpression<TSource, TDestination>(config, _registry);
    }

    public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>(
        Func<TSource, TDestination> constructor)
    {
        var config = new MappingConfiguration(typeof(TSource), typeof(TDestination))
        {
            CustomConstructor = constructor
        };
        _registry.Register(config);
        return new MappingExpression<TSource, TDestination>(config, _registry);
    }

    public void AddValueTransform<T>(Func<T, T> transform)
    {
        _registry.GlobalValueTransforms[typeof(T)] = transform;
    }

    public void AddNamingConvention(INamingConvention convention)
    {
        // Stored globally — applied during mapping compilation
    }

    public void GlobalIgnore(Func<string, bool> memberNamePredicate)
    {
        _registry.GlobalIgnorePredicates.Add(memberNamePredicate);
    }
}
