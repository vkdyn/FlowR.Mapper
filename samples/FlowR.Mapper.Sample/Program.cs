using FlowR.Mapper;
using FlowR.Mapper.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

// ============================================================
// FlowR.Mapper Sample — demonstrates full API surface
// ============================================================

var services = new ServiceCollection();
services.AddFlowRMapper(options =>
{
    options.ValidateOnStartup = false;
    options.AddProfile<ECommerceProfile>();
});

var provider = services.BuildServiceProvider();
var mapper = provider.GetRequiredService<IMapper>();

Console.WriteLine("=== FlowR.Mapper Sample ===\n");

// ---- 1. Basic mapping ----
Console.WriteLine("--- 1. Basic Mapping ---");
var userEntity = new UserEntity
{
    Id = 1, FirstName = "Krish", LastName = "Dev",
    Email = "krish@flowr.dev",
    DateOfBirth = new DateTime(1990, 5, 15),
    IsActive = true,
    Address = new Address { Street = "123 Tech Lane", City = "Auckland", PostCode = "1010" }
};
var userDto = mapper.Map<UserEntity, UserDto>(userEntity);
Console.WriteLine($"Name: {userDto.FullName}");
Console.WriteLine($"Age: {userDto.Age}");
Console.WriteLine($"City: {userDto.Address?.City}\n");

// ---- 2. Collection mapping ----
Console.WriteLine("--- 2. Collection Mapping ---");
var orders = new List<OrderEntity>
{
    new() { OrderId = 1, Total = 150.00m, Status = "Shipped" },
    new() { OrderId = 2, Total = 89.99m, Status = "Pending" },
    new() { OrderId = 3, Total = 249.99m, Status = "Delivered" }
};
var orderDtos = mapper.MapToList<OrderEntity, OrderDto>(orders);
orderDtos.ForEach(o => Console.WriteLine($"  Order #{o.OrderId}: ${o.Total} [{o.Status}]"));
Console.WriteLine();

// ---- 3. Flattened mapping ----
Console.WriteLine("--- 3. Flattened Mapping ---");
var flatDto = mapper.Map<UserEntity, UserFlatDto>(userEntity);
Console.WriteLine($"AddressCity: {flatDto.AddressCity}");
Console.WriteLine($"AddressPostCode: {flatDto.AddressPostCode}\n");

// ---- 4. Immutable record mapping ----
Console.WriteLine("--- 4. Immutable Record (ConstructUsing) ---");
var product = new ProductEntity { Id = 99, Name = "FlowR T-Shirt", Price = 29.99m, CategoryId = 5 };
var productDto = mapper.Map<ProductEntity, ProductDto>(product);
Console.WriteLine($"Product: {productDto.Name} | Price: ${productDto.Price} | Category: {productDto.Category}\n");

// ---- 5. Deep mapping (auto-recursion) ----
Console.WriteLine("--- 5. Deep Mapping ---");
var userWithOrders = new UserEntity
{
    Id = 2, FirstName = "Alice", LastName = "Smith", Email = "alice@flowr.dev",
    DateOfBirth = new DateTime(1985, 3, 10),
    Address = new Address { City = "Wellington", Street = "99 Harbor Rd", PostCode = "6011" },
    Orders = [new() { OrderId = 10, Total = 500m, Status = "Processing" }]
};
var fullDto = mapper.Map<UserEntity, UserDto>(userWithOrders);
Console.WriteLine($"{fullDto.FullName} has {fullDto.Orders.Count} order(s):");
fullDto.Orders.ForEach(o => Console.WriteLine($"  Order #{o.OrderId}: ${o.Total}"));
Console.WriteLine();

// ---- 6. Map into existing instance ----
Console.WriteLine("--- 6. Map Into Existing Instance ---");
var existingDto = new UserDto { Id = 0, Email = "old@email.com" };
mapper.Map(userEntity, existingDto);
Console.WriteLine($"Updated existing DTO Id: {existingDto.Id}, Email: {existingDto.Email}\n");

// ---- 7. HasMapping check ----
Console.WriteLine("--- 7. HasMapping ---");
Console.WriteLine($"UserEntity -> UserDto: {mapper.HasMapping<UserEntity, UserDto>()}");
Console.WriteLine($"UserEntity -> ProductDto: {mapper.HasMapping<UserEntity, ProductDto>()}");

Console.WriteLine("\n=== Done! ===");

// ============================================================
// Domain Models
// ============================================================
public class UserEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public Address? Address { get; set; }
    public List<OrderEntity> Orders { get; set; } = [];
}

public class Address { public string Street { get; set; } = ""; public string City { get; set; } = ""; public string PostCode { get; set; } = ""; }
public class OrderEntity { public int OrderId { get; set; } public decimal Total { get; set; } public string Status { get; set; } = ""; }
public class ProductEntity { public int Id { get; set; } public string Name { get; set; } = ""; public decimal Price { get; set; } public int CategoryId { get; set; } }

// ============================================================
// DTOs
// ============================================================
public class UserDto { public int Id { get; set; } public string FullName { get; set; } = ""; public string Email { get; set; } = ""; public int Age { get; set; } public AddressDto? Address { get; set; } public List<OrderDto> Orders { get; set; } = []; }
public class AddressDto { public string Street { get; set; } = ""; public string City { get; set; } = ""; public string PostCode { get; set; } = ""; }
public class OrderDto { public int OrderId { get; set; } public decimal Total { get; set; } public string Status { get; set; } = ""; }
public class UserFlatDto { public int Id { get; set; } public string Email { get; set; } = ""; public string AddressCity { get; set; } = ""; public string AddressPostCode { get; set; } = ""; }
public record ProductDto(int Id, string Name, decimal Price, string Category);

// ============================================================
// Profiles
// ============================================================
public class ECommerceProfile : MapperProfile
{
    private static readonly Dictionary<int, string> CategoryNames = new()
    {
        [1] = "Electronics", [2] = "Clothing", [3] = "Books", [4] = "Food", [5] = "Accessories"
    };

    public override void Configure(IProfileConfigurator cfg)
    {
        cfg.CreateMap<Address, AddressDto>();
        cfg.CreateMap<OrderEntity, OrderDto>();

        cfg.CreateMap<UserEntity, UserDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom((Expression<Func<UserEntity, string>>)(s => $"{s.FirstName} {s.LastName}")))
            .ForMember(d => d.Age, opt => opt.MapFrom((Expression<Func<UserEntity, int>>)(s => DateTime.Today.Year - s.DateOfBirth.Year)))
            .DeepMap();

        cfg.CreateMap<UserEntity, UserFlatDto>()
            .Flatten();

        cfg.CreateMap<ProductEntity, ProductDto>(src =>
            new ProductDto(
                src.Id,
                src.Name,
                src.Price,
                CategoryNames.GetValueOrDefault(src.CategoryId, "Unknown")));

        // Trim all strings globally
        cfg.AddValueTransform<string>(s => s?.Trim() ?? s!);
    }
}
