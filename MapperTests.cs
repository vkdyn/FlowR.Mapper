using FlowR.Mapper;
using FlowR.Mapper.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowR.Mapper.Tests;

// ======================================================
// Models
// ======================================================

public class UserEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public Address? Address { get; set; }
    public List<OrderEntity> Orders { get; set; } = [];
    public bool IsActive { get; set; }
    public decimal? Salary { get; set; }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostCode { get; set; } = "";
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostCode { get; set; } = "";
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public AddressDto? Address { get; set; }
    public List<OrderDto> Orders { get; set; } = [];
    public bool IsActive { get; set; }
}

// Flattened DTO
public class UserFlatDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string AddressCity { get; set; } = "";
    public string AddressPostCode { get; set; } = "";
}

public class OrderEntity
{
    public int OrderId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
}

public class OrderDto
{
    public int OrderId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
}

// Immutable record
public record ProductRecord(int Id, string Name, decimal Price);
public record ProductDto(int Id, string Name, decimal Price, string Category);

// ======================================================
// Profiles
// ======================================================

public class UserMappingProfile : MapperProfile
{
    public override void Configure(IProfileConfigurator cfg)
    {
        cfg.CreateMap<Address, AddressDto>();
        cfg.CreateMap<OrderEntity, OrderDto>();

        cfg.CreateMap<UserEntity, UserDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.Age, opt => opt.MapFrom(s => DateTime.Today.Year - s.DateOfBirth.Year))
            .DeepMap();

        cfg.CreateMap<UserEntity, UserFlatDto>()
            .Flatten();

        // Global: trim all strings
        cfg.AddValueTransform<string>(s => s.Trim());
    }
}

// ======================================================
// Tests
// ======================================================

public class MapperTests
{
    private IMapper BuildMapper(Action<IProfileConfigurator>? configure = null)
    {
        if (configure != null)
        {
            var services = new ServiceCollection();
            services.AddFlowRMapper(configure);
            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        var svc = new ServiceCollection();
        svc.AddFlowRMapper(typeof(MapperTests).Assembly);
        return svc.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    // ---- Basic mapping ----

    [Fact]
    public void Map_BasicProperties_AreMapped()
    {
        var mapper = BuildMapper();
        var user = new UserEntity { Id = 1, Email = "krish@test.com", IsActive = true };
        var dto = mapper.Map<UserEntity, UserDto>(user);
        Assert.Equal(1, dto.Id);
        Assert.Equal("krish@test.com", dto.Email);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void Map_CustomResolver_FullName()
    {
        var mapper = BuildMapper();
        var user = new UserEntity { FirstName = "Krish", LastName = "Dev" };
        var dto = mapper.Map<UserEntity, UserDto>(user);
        Assert.Equal("Krish Dev", dto.FullName);
    }

    [Fact]
    public void Map_ComputedProperty_Age()
    {
        var mapper = BuildMapper();
        var user = new UserEntity { DateOfBirth = new DateTime(1990, 1, 1) };
        var dto = mapper.Map<UserEntity, UserDto>(user);
        Assert.Equal(DateTime.Today.Year - 1990, dto.Age);
    }

    // ---- Deep mapping ----

    [Fact]
    public void Map_DeepMap_NestedAddressIsMapped()
    {
        var mapper = BuildMapper();
        var user = new UserEntity
        {
            Id = 1,
            Address = new Address { Street = "123 Main St", City = "Auckland", PostCode = "1010" }
        };
        var dto = mapper.Map<UserEntity, UserDto>(user);
        Assert.NotNull(dto.Address);
        Assert.Equal("Auckland", dto.Address.City);
        Assert.Equal("1010", dto.Address.PostCode);
    }

    // ---- Collection mapping ----

    [Fact]
    public void Map_Collection_OrdersAreMapped()
    {
        var mapper = BuildMapper();
        var user = new UserEntity
        {
            Orders =
            [
                new OrderEntity { OrderId = 1, Total = 99.99m, Status = "Shipped" },
                new OrderEntity { OrderId = 2, Total = 49.50m, Status = "Pending" }
            ]
        };
        var dto = mapper.Map<UserEntity, UserDto>(user);
        Assert.Equal(2, dto.Orders.Count);
        Assert.Equal(99.99m, dto.Orders[0].Total);
        Assert.Equal("Pending", dto.Orders[1].Status);
    }

    [Fact]
    public void MapToList_ReturnsCorrectCount()
    {
        var mapper = BuildMapper();
        var orders = new List<OrderEntity>
        {
            new() { OrderId = 1, Total = 10m },
            new() { OrderId = 2, Total = 20m },
            new() { OrderId = 3, Total = 30m }
        };
        var dtos = mapper.MapToList<OrderEntity, OrderDto>(orders);
        Assert.Equal(3, dtos.Count);
        Assert.Equal(30m, dtos[2].Total);
    }

    [Fact]
    public void MapToArray_ReturnsArray()
    {
        var mapper = BuildMapper();
        var orders = new List<OrderEntity> { new() { OrderId = 1 }, new() { OrderId = 2 } };
        var arr = mapper.MapToArray<OrderEntity, OrderDto>(orders);
        Assert.IsType<OrderDto[]>(arr);
        Assert.Equal(2, arr.Length);
    }

    // ---- Null handling ----

    [Fact]
    public void Map_NullSource_ReturnsDefault()
    {
        var mapper = BuildMapper();
        UserEntity? nullUser = null;
        var dto = mapper.Map<UserEntity, UserDto>(nullUser!);
        Assert.Null(dto);
    }

    // ---- Flatten ----

    [Fact]
    public void Map_Flatten_NestedPropertiesMapped()
    {
        var mapper = BuildMapper();
        var user = new UserEntity
        {
            Id = 5,
            Email = "flat@test.com",
            Address = new Address { City = "Wellington", PostCode = "6011" }
        };
        var dto = mapper.Map<UserEntity, UserFlatDto>(user);
        Assert.Equal("Wellington", dto.AddressCity);
        Assert.Equal("6011", dto.AddressPostCode);
        Assert.Equal(5, dto.Id);
    }

    // ---- Ignore ----

    [Fact]
    public void Map_IgnoredMember_IsNotMapped()
    {
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<UserEntity, UserDto>()
                .Ignore(d => d.Email);
        });

        var user = new UserEntity { Id = 1, Email = "secret@test.com" };
        var dto = mapper.Map<UserEntity, UserDto>(user);
        Assert.Null(dto.Email); // default value, not mapped
    }

    // ---- ConstructUsing (immutable records) ----

    [Fact]
    public void Map_ConstructUsing_ImmutableRecord()
    {
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<ProductRecord, ProductDto>(src =>
                new ProductDto(src.Id, src.Name, src.Price, "General"));
        });

        var product = new ProductRecord(1, "Widget", 9.99m);
        var dto = mapper.Map<ProductRecord, ProductDto>(product);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Widget", dto.Name);
        Assert.Equal("General", dto.Category);
    }

    // ---- Conditional mapping ----

    [Fact]
    public void Map_MemberCondition_SkipsWhenFalse()
    {
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<UserEntity, UserDto>()
                .ForMember(d => d.Email, opt =>
                {
                    opt.MapFrom(s => s.Email);
                    opt.Condition(s => s.IsActive);
                });
        });

        var inactiveUser = new UserEntity { Email = "inactive@test.com", IsActive = false };
        var dto = mapper.Map<UserEntity, UserDto>(inactiveUser);
        Assert.Null(dto.Email); // Skipped because IsActive is false
    }

    // ---- Before/After hooks ----

    [Fact]
    public void Map_BeforeAfterHooks_AreCalled()
    {
        var log = new List<string>();

        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<UserEntity, UserDto>()
                .BeforeMap((src, dest) => log.Add("Before"))
                .AfterMap((src, dest) => log.Add("After"));
        });

        mapper.Map<UserEntity, UserDto>(new UserEntity());
        Assert.Equal(["Before", "After"], log);
    }

    // ---- ReverseMap ----

    [Fact]
    public void ReverseMap_RegistersReverseMapping()
    {
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<OrderEntity, OrderDto>().ReverseMap();
        });

        Assert.True(mapper.HasMapping<OrderEntity, OrderDto>());
        Assert.True(mapper.HasMapping<OrderDto, OrderEntity>());
    }

    // ---- HasMapping ----

    [Fact]
    public void HasMapping_ReturnsTrueForRegistered()
    {
        var mapper = BuildMapper();
        Assert.True(mapper.HasMapping<UserEntity, UserDto>());
        Assert.False(mapper.HasMapping<UserEntity, ProductRecord>());
    }

    // ---- MappingNotFoundException ----

    [Fact]
    public void Map_NoMapping_ThrowsMappingNotFoundException()
    {
        var mapper = BuildMapper(cfg => { }); // Empty — no mappings
        Assert.Throws<MappingNotFoundException>(() =>
            mapper.Map<UserEntity, UserDto>(new UserEntity()));
    }

    // ---- Map into existing instance ----

    [Fact]
    public void Map_IntoExisting_UpdatesDestination()
    {
        var mapper = BuildMapper();
        var user = new UserEntity { Id = 42, Email = "update@test.com" };
        var existing = new UserDto { Id = 0, Email = "old@test.com" };

        var result = mapper.Map<UserEntity, UserDto>(user, existing);
        Assert.Equal(42, result.Id);
        Assert.Equal("update@test.com", result.Email);
        Assert.Same(existing, result); // Same instance
    }

    // ---- Global value transform ----

    [Fact]
    public void Map_GlobalStringTrim_TrimsWhitespace()
    {
        var mapper = BuildMapper(cfg =>
        {
            cfg.AddValueTransform<string>(s => s.Trim());
            cfg.CreateMap<UserEntity, UserDto>();
        });

        var user = new UserEntity { Email = "   padded@test.com   " };
        var dto = mapper.Map<UserEntity, UserDto>(user);
        Assert.Equal("padded@test.com", dto.Email);
    }

    // ---- Type-based Map (no TSource generic) ----

    [Fact]
    public void Map_ByObjectSource_Works()
    {
        var mapper = BuildMapper();
        object user = new OrderEntity { OrderId = 7, Total = 55m, Status = "Complete" };
        var dto = mapper.Map<OrderDto>(user);
        Assert.Equal(7, dto.OrderId);
        Assert.Equal(55m, dto.Total);
    }
}
