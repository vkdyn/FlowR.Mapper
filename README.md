# FlowR.Mapper - Organized File Structure

```
FlowR.Mapper/
├── src/FlowR.Mapper/
│   │
│   ├── 📁 Interfaces/
│   │   ├── IMapper.cs                         # Main mapper interface
│   │   ├── IMappingExpression.cs              # Fluent configuration API
│   │   ├── IMappingAction.cs                  # ⭐ NEW - Reusable mapping actions
│   │   └── ITypeConverter.cs                  # Type converter interface (if exists)
│   │
│   ├── 📁 Configuration/
│   │   ├── MapperProfile.cs                   # Base class for profiles
│   │   ├── ProfileConfigurator.cs             # Profile configuration helper
│   │   ├── MappingExpression.cs               # IMappingExpression implementation
│   │   └── NamingConventions.cs               # Naming conventions
│   │
│   ├── 📁 Internal/
│   │   ├── MappingConfiguration.cs            # Internal config storage
│   │   ├── MappingActionWrapper.cs            # ⭐ NEW - Action wrappers
│   │   ├── MapperRegistry.cs                  # Registry for mappings
│   │   └── ProjectionBuilder.cs               # IQueryable projection builder
│   │
│   ├── 📁 Core/
│   │   ├── FlowRMapper.cs                     # Main mapper implementation
│   │   └── ResolutionContext.cs               # ⭐ NEW - Mapping context
│   │
│   ├── 📁 Extensions/
│   │   ├── ServiceCollectionExtensions.cs     # DI registration
│   │   └── QueryableMappingExtensions.cs      # IQueryable.ProjectTo extensions
│   │
│   └── 📁 Exceptions/
│       └── Exceptions.cs                      # All exception types
│
├── tests/FlowR.Mapper.Tests/
│   ├── MapperTests.cs                         # Main test suite
│   ├── 📁 Models/
│   │   ├── Address.cs
│   │   ├── AddressDto.cs
│   │   ├── OrderEntity.cs
│   │   ├── OrderDto.cs
│   │   ├── UserEntity.cs
│   │   ├── UserDto.cs
│   │   └── UserFlatDto.cs
│   │
│   └── 📁 NewFeatures/                        # Optional: Separate new feature tests
│       └── NewFeatureTests.cs
│
└── samples/FlowR.Mapper.Sample/
    └── Program.cs
```

## Recommended Folder Organization

### 📁 **Interfaces/** - Public contracts
```
All public-facing interfaces that users interact with
- IMapper.cs
- IMappingExpression.cs
- IMappingAction.cs
- ITypeConverter.cs (if you have it)
```

### 📁 **Configuration/** - Mapping setup
```
Everything related to configuring mappings
- MapperProfile.cs
- ProfileConfigurator.cs
- MappingExpression.cs
- NamingConventions.cs
```

### 📁 **Internal/** - Implementation details
```
Internal classes (marked with 'internal' keyword)
- MappingConfiguration.cs
- MappingActionWrapper.cs
- MapperRegistry.cs
- ProjectionBuilder.cs
```

### 📁 **Core/** - Main engine
```
Core mapper implementation
- FlowRMapper.cs
- ResolutionContext.cs
```

### 📁 **Extensions/** - Extension methods
```
All extension method classes
- ServiceCollectionExtensions.cs
- QueryableMappingExtensions.cs
```

### 📁 **Exceptions/** - Exception types
```
All custom exception classes
- Exceptions.cs (or split into individual files)
```

## Alternative Simpler Structure

If you prefer fewer folders:

```
FlowR.Mapper/
├── src/FlowR.Mapper/
│   │
│   ├── 📁 Abstractions/              # Interfaces only
│   │   ├── IMapper.cs
│   │   ├── IMappingExpression.cs
│   │   └── IMappingAction.cs
│   │
│   ├── 📁 Configuration/             # Profiles & setup
│   │   ├── MapperProfile.cs
│   │   ├── MappingExpression.cs
│   │   └── ProfileConfigurator.cs
│   │
│   ├── 📁 Internal/                  # Implementation
│   │   ├── FlowRMapper.cs
│   │   ├── MappingConfiguration.cs
│   │   ├── MappingActionWrapper.cs
│   │   ├── MapperRegistry.cs
│   │   ├── ProjectionBuilder.cs
│   │   └── ResolutionContext.cs
│   │
│   ├── 📁 Extensions/                # Extension methods
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── QueryableMappingExtensions.cs
│   │
│   ├── Exceptions.cs
│   └── NamingConventions.cs
```

## Migration Steps

### Step 1: Create Folders
```bash
mkdir src/FlowR.Mapper/Interfaces
mkdir src/FlowR.Mapper/Configuration
mkdir src/FlowR.Mapper/Internal
mkdir src/FlowR.Mapper/Core
mkdir src/FlowR.Mapper/Extensions
mkdir src/FlowR.Mapper/Exceptions
```

### Step 2: Move Files
```bash
# Interfaces
mv IMapper.cs Interfaces/
mv IMappingExpression.cs Interfaces/
mv IMappingAction.cs Interfaces/

# Configuration
mv MapperProfile.cs Configuration/
mv ProfileConfigurator.cs Configuration/
mv MappingExpression.cs Configuration/
mv NamingConventions.cs Configuration/

# Internal
mv MappingConfiguration.cs Internal/
mv MappingActionWrapper.cs Internal/
mv MapperRegistry.cs Internal/
mv ProjectionBuilder.cs Internal/

# Core
mv FlowRMapper.cs Core/
mv ResolutionContext.cs Core/

# Extensions
mv ServiceCollectionExtensions.cs Extensions/
mv QueryableMappingExtensions.cs Extensions/

# Exceptions
mv Exceptions.cs Exceptions/
```

### Step 3: Update Namespaces

All files should have:
```csharp
namespace FlowR.Mapper.{FolderName};

// Examples:
namespace FlowR.Mapper.Interfaces;
namespace FlowR.Mapper.Configuration;
namespace FlowR.Mapper.Internal;
namespace FlowR.Mapper.Core;
namespace FlowR.Mapper.Extensions;
namespace FlowR.Mapper.Exceptions;
```

### Step 4: Update .csproj (if needed)

The .csproj should auto-detect the new structure, but verify:
```xml
<ItemGroup>
  <Compile Include="Interfaces\*.cs" />
  <Compile Include="Configuration\*.cs" />
  <Compile Include="Internal\*.cs" />
  <Compile Include="Core\*.cs" />
  <Compile Include="Extensions\*.cs" />
  <Compile Include="Exceptions\*.cs" />
</ItemGroup>
```

## Namespace Usage After Reorganization

Users will need to add using statements:
```csharp
using FlowR.Mapper;                    // Still the main namespace
using FlowR.Mapper.Interfaces;         // For IMapper, IMappingAction
using FlowR.Mapper.Configuration;      // For MapperProfile
using FlowR.Mapper.Extensions;         // For .AddFlowRMapper(), .ProjectTo()
```

**OR** keep backward compatibility by adding global usings in a common file:

**GlobalUsings.cs**:
```csharp
global using FlowR.Mapper.Interfaces;
global using FlowR.Mapper.Configuration;
global using FlowR.Mapper.Extensions;
global using FlowR.Mapper.Exceptions;
```

## Benefits of This Structure

✅ **Clear separation of concerns**
✅ **Easy to navigate** - find interfaces in one place
✅ **Better encapsulation** - Internal folder is clearly internal
✅ **Scalability** - Easy to add new features in right place
✅ **Standard .NET conventions** - Follows common patterns

## Recommendation

I suggest the **6-folder structure** (Interfaces, Configuration, Internal, Core, Extensions, Exceptions) as it provides the best organization without being overly complex. It's clean, professional, and follows .NET library conventions.