# FlowR.Mapper - Complete AutoMapper Alternative

[![NuGet](https://img.shields.io/badge/NuGet-v1.0.0-blue.svg)](https://www.nuget.org/packages/FlowR.Mapper)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**The best free object mapper for .NET — 95%+ AutoMapper parity, MIT licensed, zero cost.**

---

## 🎯 What's New - Complete Feature Parity

This version includes **ALL** the missing AutoMapper features:

### ✅ New Features Added

1. **ForPath** - Map to nested destination properties ⭐
2. **IValueResolver** - Reusable member resolver classes ⭐
3. **SetMappingOrder** - Control property mapping order ⭐
4. **UseDestinationValue** - Preserve existing values ⭐
5. **ForAllOtherMembers** - Config for unmapped members ⭐

---

## 🚀 Quick Start

```bash
dotnet add package FlowR.Mapper
```

```csharp
// 1. Create profile
public class UserProfile : MapperProfile
{
    public override void Configure(IProfileConfigurator cfg)
    {
        cfg.CreateMap<UserEntity, UserDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
    }
}

// 2. Register
builder.Services.AddFlowRMapper(typeof(Program).Assembly);

// 3. Use
mapper.Map<UserEntity, UserDto>(user);
```

---

## 📚 All Features - 95% AutoMapper Parity

✅ ForMember • ForPath • ForAllMembers • ForAllOtherMembers  
✅ Ignore • ReverseMap • Flatten • DeepMap  
✅ IncludeBase • PreCondition • AllowNull  
✅ IValueResolver • IMappingAction • ResolutionContext  
✅ SetMappingOrder • UseDestinationValue  
✅ ConvertUsing • ConstructUsing • ProjectTo  
✅ ValidateAllMembers • MaxDepth • NullSubstitute  

---

## 📁 Clean Code Organization

```
src/FlowR.Mapper/
├── Interfaces/          # Public contracts (IMapper, IValueResolver)
├── Configuration/       # Mapping setup (MapperProfile)
├── Internal/            # Implementation details
├── Core/                # Main engine (FlowRMapper, ResolutionContext)
├── Extensions/          # Extension methods
└── Exceptions/          # Exception types

tests/FlowR.Mapper.Tests/
├── MapperTests.cs                      # 38+ core tests
├── AdvancedMappingFeaturesTests.cs     # Advanced feature tests
├── TestModels/                         # Organized test models
├── Resolvers/                          # Reusable resolvers
└── Actions/                            # Mapping actions
```

---

## 💡 Example: All Features Combined

```csharp
public class EmployeeProfile : MapperProfile
{
    public override void Configure(IProfileConfigurator cfg)
    {
        cfg.CreateMap<EmployeeEntity, EmployeeDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom<FullNameResolver>())
            .ForPath(d => d.Address.City, opt => opt.MapFrom(s => s.Contact.Address.City))
            .SetMappingOrder(d => d.Salary, 1)
            .ForMember(d => d.LastModified, opt => opt.UseDestinationValue())
            .PreCondition(src => src.IsActive)
            .AfterMap((src, dest, ctx) => dest.ProcessedAt = DateTime.UtcNow);
    }
}
```

---

## 📝 License

MIT - Free for personal and commercial use

**Built with ❤️ for the .NET community**
