# Changelog

All notable changes to FlowR.Mapper will be documented in this file.

## [1.1.0] - 2026-04-24

### Added - Complete AutoMapper Parity (95%+)

#### 🎯 High-Priority Features
- **ForPath** - Map to nested destination properties
  - `ForPath(d => d.Address.City, opt => opt.MapFrom(s => s.Contact.Address.City))`
  - Support for nested path mapping with automatic instantiation
  - Constant value support: `UseValue()`
  
- **IValueResolver** - Reusable member resolver classes
  - New interface for creating testable, reusable resolvers
  - `MapFrom<TResolver>()` support
  - Full ResolutionContext access in resolvers
  
- **SetMappingOrder** - Control property mapping order
  - Map properties in specific order
  - Support for dependent property calculations
  - Both expression-level and member-level API
  
- **UseDestinationValue** - Preserve existing destination values
  - Keep existing values instead of mapping from source
  - Useful for preserving timestamps, audit fields
  
- **ForAllOtherMembers** - Apply configuration to unmapped members
  - Complement to ForAllMembers
  - Configure remaining members after explicit configuration

### Changed

#### Code Organization
- Created organized folder structure:
  - `Interfaces/` - All public contracts
  - `Configuration/` - Mapping setup classes
  - `Internal/` - Implementation details
  - `Core/` - Main mapping engine
  - `Extensions/` - Extension methods
  
#### Test Organization
- Created dedicated test model files:
  - `TestModels/` - Clean model organization
  - `Resolvers/` - Reusable resolver implementations
  - `Actions/` - Mapping action classes
- Added `AdvancedMappingFeaturesTests.cs` with comprehensive tests

### Enhanced

#### MappingConfiguration
- Added `PathResolvers` dictionary for ForPath support
- Added `PathConstants` dictionary for constant path values
- Added `ValueResolvers` dictionary for IValueResolver instances
- Added `UseDestinationValueMembers` set for preserving values
- Added `MemberMappingOrder` dictionary for ordering
- Added `ForAllOtherMembersAction` delegate

#### FlowRMapper
- Enhanced MapProperties method:
  - Property sorting by mapping order
  - UseDestinationValue handling
  - IValueResolver execution
  - ForPath nested property setting
  - Configuration tracking for ForAllOtherMembers
- Added `SetNestedProperty` method for path-based mapping

#### IMappingExpression
- Added `ForPath<TMember>` method
- Added `ForAllOtherMembers` method
- Added `IPathOptions` interface

#### IMemberOptions
- Added `MapFrom<TResolver>()` overloads
- Added `UseDestinationValue()` method
- Added `SetMappingOrder(int)` method

### Test Coverage

#### New Tests
- `ForPath_MapsToNestedProperty`
- `ForPath_WithConstantValue`
- `ValueResolver_ResolvesUsingCustomClass`
- `SetMappingOrder_MapsPropertiesInSpecifiedOrder`
- `UseDestinationValue_PreservesExistingValue`
- `ForAllOtherMembers_AppliesConfigToUnmappedMembers`

#### Test Utilities
- `FullNameResolver` - Example IValueResolver
- `SalaryFormatter` - Currency formatting resolver
- `DepartmentCodeResolver` - Code generation resolver
- `ContextAwareResolver` - Context-aware resolver

### Documentation
- Updated README with all new features
- Added comprehensive usage examples
- Created feature comparison table
- Documented code organization

---

## [1.0.0] - 2026-04-23

### Initial Release

#### Core Features
- ForMember - Custom member mapping
- Ignore - Skip members
- ReverseMap - Bidirectional mapping
- Flatten - Nested property flattening
- DeepMap - Recursive mapping
- ConstructUsing - Custom constructors
- Collections - List, Array, IEnumerable
- Validation - ValidateAllMembersAreMapped

#### Advanced Features
- ForAllMembers - Apply config to all members
- IncludeBase - Inherit base mappings
- PreCondition - Conditional mapping (3 overloads)
- AllowNull - Null propagation control
- AfterMap/BeforeMap - With ResolutionContext
- ConvertUsing - Custom conversions (3 overloads)
- IMappingAction - Reusable action classes
- ResolutionContext - Full mapping context

#### EF Core Integration
- ProjectTo - IQueryable projection
- Expression tree support

#### DI Integration
- Service collection extensions
- Profile scanning
- Profile-based configuration

---

## Upgrade Guide

### From 1.0.0 to 1.1.0

No breaking changes. All new features are additive.

#### New APIs Available:
```csharp
// ForPath
.ForPath(d => d.Address.City, opt => opt.MapFrom(s => s.City))

// IValueResolver
public class MyResolver : IValueResolver<Source, Dest, string> { ... }
.ForMember(d => d.Field, opt => opt.MapFrom<MyResolver>())

// SetMappingOrder
.SetMappingOrder(d => d.Field, 1)

// UseDestinationValue
.ForMember(d => d.LastUpdated, opt => opt.UseDestinationValue())

// ForAllOtherMembers
.ForAllOtherMembers(opt => opt.Condition(s => s != null))
```

---

## Future Roadmap

### Possible Future Features
- Inline mapping options (Map() time configuration)
- BeforeMap/AfterMap at Map() call time
- ForSourceMember (reverse mapping control)
- ExplicitExpansion (EF Core scenarios)
- Additional performance optimizations

---

**Note**: FlowR.Mapper maintains 95%+ feature parity with AutoMapper while remaining free and open source under the MIT license.
