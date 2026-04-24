namespace FlowR.Mapper;

/// <summary>
/// The core mapper. Inject this anywhere you need object mapping.
/// </summary>
public interface IMapper
{
    /// <summary>Maps source to a new instance of TDestination.</summary>
    TDestination Map<TDestination>(object source);

    /// <summary>Maps source to a new instance of TDestination (strongly typed source).</summary>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>Maps source into an existing destination instance.</summary>
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>Maps a collection of TSource to IEnumerable of TDestination.</summary>
    IEnumerable<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source);

    /// <summary>Maps a collection to a List.</summary>
    List<TDestination> MapToList<TSource, TDestination>(IEnumerable<TSource> source);

    /// <summary>Maps a collection to an array.</summary>
    TDestination[] MapToArray<TSource, TDestination>(IEnumerable<TSource> source);

    /// <summary>
    /// Projects a queryable for use in EF Core / LINQ queries.
    /// Generates efficient SQL — only selects the columns you need.
    /// </summary>
    IQueryable<TDestination> ProjectTo<TSource, TDestination>(IQueryable<TSource> source);

    /// <summary>Checks whether a mapping exists between two types.</summary>
    bool HasMapping<TSource, TDestination>();

    /// <summary>Validates all registered mappings. Call at startup to catch config errors early.</summary>
    void AssertConfigurationIsValid();
}
