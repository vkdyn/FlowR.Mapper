using System.Linq.Expressions;

namespace FlowR.Mapper;

/// <summary>
/// Define your mappings by inheriting from <see cref="MapperProfile"/>.
/// Group related mappings together (e.g., UserProfile, OrderProfile).
/// </summary>
public interface IMapperProfile
{
    void Configure(IProfileConfigurator configurator);
}

/// <summary>
/// The fluent API exposed inside a profile for registering mappings.
/// </summary>
public interface IProfileConfigurator
{
    /// <summary>
    /// Creates a mapping from TSource to TDestination.
    /// Returns a fluent expression for further configuration.
    /// </summary>
    IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();

    /// <summary>
    /// Creates a mapping using a constructor expression (ideal for records/immutable types).
    /// </summary>
    IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>(
        Func<TSource, TDestination> constructor);

    /// <summary>
    /// Registers a global value transformer applied to all members of a given type.
    /// E.g., trim all strings, round all decimals.
    /// </summary>
    void AddValueTransform<T>(Func<T, T> transform);

    /// <summary>
    /// Adds a naming convention for automatic property matching.
    /// </summary>
    void AddNamingConvention(INamingConvention convention);

    /// <summary>
    /// Globally ignores members matching a condition across all mappings.
    /// </summary>
    void GlobalIgnore(Func<string, bool> memberNamePredicate);
}

/// <summary>
/// A naming convention for automatic property matching.
/// </summary>
public interface INamingConvention
{
    /// <summary>Converts a source member name to the destination member name.</summary>
    string Convert(string sourceName);
}

/// <summary>
/// Base class for defining mapper profiles. Inherit from this.
/// </summary>
public abstract class MapperProfile : IMapperProfile
{
    public abstract void Configure(IProfileConfigurator configurator);
}

/// <summary>
/// A type converter for custom type conversion logic.
/// </summary>
public interface ITypeConverter<in TSource, out TDestination>
{
    TDestination Convert(TSource source);
}
