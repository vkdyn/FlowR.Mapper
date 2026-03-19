using System.Linq.Expressions;
using System.Reflection;

namespace FlowR.Mapper.Internal;

/// <summary>
/// Internal configuration for a single TSource -> TDestination mapping.
/// </summary>
internal sealed class MappingConfiguration
{
    public Type SourceType { get; }
    public Type DestinationType { get; }

    // Member-level overrides: destination property name -> resolver func
    public Dictionary<string, Delegate> MemberResolvers { get; } = new();
    // Ignored destination member names
    public HashSet<string> IgnoredMembers { get; } = new();
    // Conditional member mapping: dest member name -> condition
    public Dictionary<string, Delegate> MemberConditions { get; } = new();
    // Constant member values
    public Dictionary<string, object?> MemberConstants { get; } = new();
    // Null substitutes per member
    public Dictionary<string, object?> MemberNullSubstitutes { get; } = new();

    // Global condition on the whole mapping
    public Delegate? GlobalCondition { get; set; }

    // Before/after hooks
    public List<Delegate> BeforeMapActions { get; } = [];
    public List<Delegate> AfterMapActions { get; } = [];

    // Custom constructor
    public Delegate? CustomConstructor { get; set; }

    // Flatten nested properties
    public bool FlattenEnabled { get; set; }
    public bool DeepMapEnabled { get; set; } = true;
    public bool ReverseMapEnabled { get; set; }
    public bool ValidateAllMembers { get; set; }
    public int MaxDepth { get; set; } = 5;
    public object? NullSubstitute { get; set; }

    // Derived type mappings: (derivedSource, derivedDest)
    public List<(Type DerivedSource, Type DerivedDest)> DerivedTypeMappings { get; } = [];

    // Custom type converter (overrides all member mapping)
    public Delegate? TypeConverter { get; set; }

    // Compiled mapping function cache
    private Delegate? _compiledMapper;
    private Delegate? _compiledMergeMapper;

    public MappingConfiguration(Type sourceType, Type destinationType)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    public void SetCompiledMapper(Delegate mapper) => _compiledMapper = mapper;
    public Delegate? GetCompiledMapper() => _compiledMapper;
    public void SetCompiledMergeMapper(Delegate mapper) => _compiledMergeMapper = mapper;
    public Delegate? GetCompiledMergeMapper() => _compiledMergeMapper;

    public (Type, Type) Key => (SourceType, DestinationType);
}
