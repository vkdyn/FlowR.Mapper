# FlowR.Mapper 🗺️

[![NuGet](https://img.shields.io/nuget/v/FlowR.Mapper.svg)](https://www.nuget.org/packages/FlowR.Mapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FlowR.Mapper.svg)](https://www.nuget.org/packages/FlowR.Mapper)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![CI](https://github.com/yourusername/FlowR/actions/workflows/ci-mapper.yml/badge.svg)](https://github.com/yourusername/FlowR/actions)

**The best free object mapper for .NET — more features, simpler API, zero cost.**

FlowR.Mapper is a high-performance, open-source alternative to AutoMapper. Map objects, flatten nested types, deep-map hierarchies, project to IQueryable (EF Core), and validate your configuration — all with a clean, fluent API.

---

## Why FlowR.Mapper over AutoMapper?

| Feature | FlowR.Mapper | AutoMapper |
|---|---|---|
| License | ✅ MIT (free forever) | ❌ Commercial license |
| Basic mapping | ✅ | ✅ |
| Custom resolvers | ✅ | ✅ |
| Ignore members | ✅ | ✅ |
| Deep/nested mapping | ✅ Auto-detected | ✅ |
| Flattening | ✅ | ✅ |
| ReverseMap | ✅ | ✅ |
| Collection mapping | ✅ List, Array, IEnumerable | ✅ |
| IQueryable projection (EF Core) | ✅ | ✅ |
| ConstructUsing (records/immutable) | ✅ | ✅ |
| Conditional member mapping | ✅ | ✅ |
| Before/After hooks | ✅ | ✅ |
| Global value transforms | ✅ | ✅ |
| Global ignore predicates | ✅ | ❌ |
| Map into existing instance | ✅ | ✅ |
| Polymorphic/derived type mapping | ✅ | ✅ |
| Null substitute per member | ✅ | ✅ |
| Type converters | ✅ | ✅ |
| Naming conventions (snake_case etc.) | ✅ Built-in | ✅ |
| Validate all members mapped | ✅ | ✅ |
| Map<TDest>(object) non-generic | ✅ | ✅ |
| Max depth control | ✅ | ❌ |
| Clear error messages | ✅ | ⚠️ |
| Assembly scanning for profiles | ✅ | ✅ |

---

## Installation

```bash
dotnet add package FlowR.Mapper
```

---

## Quick Start

### 1. Create a Profile

```csharp
public class UserProfile : MapperProfile
{
    public override void Configure(IProfileConfigurator cfg)
    {
        cfg.CreateMap<UserEntity, UserDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.Age, opt => opt.MapFrom(s => DateTime.Today.Year - s.DateOfBirth.Year))
            .DeepMap(); // Auto-maps nested Address -> AddressDto if registered

        cfg.CreateMap<Address, AddressDto>();
        cfg.CreateMap<OrderEntity, OrderDto>();
    }
}
```

### 2. Register

```csharp
builder.Services.AddFlowRMapper(options =>
{
    options.ValidateOnStartup = true; // Catch config errors at startup
},
typeof(Program).Assembly);
```

### 3. Inject and Use

```csharp
public class UserService
{
    private readonly IMapper _mapper;
    public UserService(IMapper mapper) => _mapper = mapper;

    public UserDto GetUser(UserEntity user) => _mapper.Map<UserEntity, UserDto>(user);

    public List<UserDto> GetUsers(List<UserEntity> users)
        => _mapper.MapToList<UserEntity, UserDto>(users);
}
```

---

## All Features

### Custom Resolvers

```csharp
cfg.CreateMap<UserEntity, UserDto>()
    .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
    .ForMember(d => d.DisplayPrice, opt => opt.MapFrom((src, dest) => src.Price * dest.Multiplier))
    .ForMember(d => d.Region, opt => opt.UseValue("APAC")); // Constant value
```

### Ignore Members

```csharp
cfg.CreateMap<UserEntity, UserDto>()
    .Ignore(d => d.InternalNotes)
    .Ignore(d => d.PasswordHash);
```

### Flattening

```csharp
// source.Address.City -> destination.AddressCity (auto-detected by naming)
cfg.CreateMap<UserEntity, UserFlatDto>().Flatten();
```

### Deep Mapping

```csharp
cfg.CreateMap<Address, AddressDto>(); // Register nested mapping
cfg.CreateMap<UserEntity, UserDto>().DeepMap(); // FlowR auto-recurses
```

### Immutable Types / Records

```csharp
cfg.CreateMap<ProductEntity, ProductDto>(src =>
    new ProductDto(src.Id, src.Name, src.Price, GetCategory(src.CategoryId)));
```

### Collection Mapping

```csharp
var list = mapper.MapToList<OrderEntity, OrderDto>(orders);
var arr  = mapper.MapToArray<OrderEntity, OrderDto>(orders);
var enu  = mapper.MapList<OrderEntity, OrderDto>(orders); // IEnumerable<T>
```

### EF Core Projections

```csharp
// Only queries the columns needed — no SELECT *
var dtos = await dbContext.Users
    .Where(u => u.IsActive)
    .ProjectTo<UserEntity, UserDto>(mapper) // Or use mapper.ProjectTo<>()
    .ToListAsync();
```

### Conditional Mapping

```csharp
cfg.CreateMap<UserEntity, UserDto>()
    .ForMember(d => d.Email, opt =>
    {
        opt.MapFrom(s => s.Email);
        opt.Condition(s => s.IsActive); // Only map if active
    })
    .When(s => s.Id > 0); // Global condition for entire mapping
```

### Before/After Hooks

```csharp
cfg.CreateMap<UserEntity, UserDto>()
    .BeforeMap((src, dest) => _auditLog.Record(src.Id))
    .AfterMap((src, dest) => dest.DisplayName = dest.FullName.ToUpper());
```

### ReverseMap

```csharp
cfg.CreateMap<OrderEntity, OrderDto>().ReverseMap();
// Now both OrderEntity->OrderDto and OrderDto->OrderEntity are registered
```

### Global Value Transforms

```csharp
// Applied to ALL string properties across ALL mappings
cfg.AddValueTransform<string>(s => s?.Trim() ?? s);
cfg.AddValueTransform<decimal>(d => Math.Round(d, 2));
```

### Global Ignore

```csharp
// Ignore any member named "CreatedAt" or "UpdatedAt" everywhere
cfg.GlobalIgnore(name => name.EndsWith("At") && name.StartsWith("Created") || name.StartsWith("Updated"));
```

### Max Depth (circular references)

```csharp
cfg.CreateMap<CategoryEntity, CategoryDto>()
    .DeepMap()
    .MaxDepth(3); // Stop recursing after 3 levels
```

### Null Substitutes

```csharp
cfg.CreateMap<UserEntity, UserDto>()
    .ForMember(d => d.Email, opt => opt.NullSubstitute("noreply@example.com"));
```

### Naming Conventions

```csharp
cfg.AddNamingConvention(new SnakeCaseToPascalCaseConvention()); // first_name -> FirstName
cfg.AddNamingConvention(new CamelCaseToPascalCaseConvention()); // firstName -> FirstName
```

### Validate Configuration

```csharp
// At startup — throws MapperConfigurationException listing all unmapped members
mapper.AssertConfigurationIsValid();

// Or inline when defining the mapping
cfg.CreateMap<UserEntity, UserDto>().ValidateAllMembersAreMapped();
```

---

## Migration from AutoMapper

Most code is a direct 1:1 swap:

```csharp
// AutoMapper
services.AddAutoMapper(typeof(Program));
_mapper.Map<UserDto>(user);

// FlowR.Mapper
services.AddFlowRMapper(typeof(Program).Assembly);
_mapper.Map<UserEntity, UserDto>(user);
```

Profile syntax:

```csharp
// AutoMapper
CreateMap<UserEntity, UserDto>()
    .ForMember(d => d.Name, opt => opt.MapFrom(s => s.FirstName));

// FlowR.Mapper — identical structure
cfg.CreateMap<UserEntity, UserDto>()
    .ForMember(d => d.Name, opt => opt.MapFrom(s => s.FirstName));
```

---

## License

MIT — free for personal and commercial use, forever.

---

## Part of the FlowR Ecosystem

| Package | Description |
|---|---|
| `FlowR` | Free MediatR alternative — CQRS, requests, notifications, pipeline |
| `FlowR.Mapper` | Free AutoMapper alternative — object mapping, projections, transforms |

Star ⭐ the repo to support free open-source .NET tooling!
