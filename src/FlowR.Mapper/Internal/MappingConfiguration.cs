using FlowR.Mapper.Interfaces;
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
    // Expression-based resolvers — stored alongside compiled Func for ProjectTo SQL translation
    public Dictionary<string, LambdaExpression> MemberExpressions { get; } = new();
    // Ignored destination member names
    public HashSet<string> IgnoredMembers { get; } = new();
    // Conditional member mapping: dest member name -> condition
    public Dictionary<string, Delegate> MemberConditions { get; } = new();
    // Member-level preconditions: evaluated before resolving the destination member.
    public Dictionary<string, Delegate> MemberPreConditions { get; } = new();
    // Members configured to allow null source values to overwrite destination values.
    public HashSet<string> AllowNullMembers { get; } = new();
    // Constant member values
    public Dictionary<string, object?> MemberConstants { get; } = new();
    // Null substitutes per member
    public Dictionary<string, object?> MemberNullSubstitutes { get; } = new();

    // Global condition on the whole mapping
    public Delegate? GlobalCondition { get; set; }

    // PreCondition - must be satisfied before mapping begins
    public Delegate? PreCondition { get; set; }

    // Allow null source values (don't substitute with default)
    public bool AllowNullSource { get; set; }

    // ForAllMembers configuration action
    public Delegate? ForAllMembersAction { get; set; }

    // ForAllOtherMembers configuration action
    public Delegate? ForAllOtherMembersAction { get; set; }

    // Path-based resolvers: "Address.City" -> resolver func
    public Dictionary<string, Delegate> PathResolvers { get; } = new();

    // Path constants: "Address.City" -> constant value
    public Dictionary<string, object?> PathConstants { get; } = new();

    // Value resolvers: member name -> IValueResolver instance
    public Dictionary<string, object> ValueResolvers { get; } = new();

    // Members that should use destination value instead of mapping
    public HashSet<string> UseDestinationValueMembers { get; } = new();

    // Member mapping order: member name -> order (lower = first)
    public Dictionary<string, int> MemberMappingOrder { get; } = new();

    // Before/after hooks - using wrappers to support both 2-param and 3-param signatures
    public List<IMappingActionWrapper> BeforeMapActions { get; } = [];
    public List<IMappingActionWrapper> AfterMapActions { get; } = [];

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

    // Base type mappings: (baseSource, baseDest) - for IncludeBase
    public List<(Type BaseSource, Type BaseDest)> BaseTypeMappings { get; } = [];

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