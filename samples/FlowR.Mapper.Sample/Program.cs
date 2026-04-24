using FlowR.Mapper;
using FlowR.Mapper.Extensions;
using FlowR.Mapper.Sample.MappingActions;
using FlowR.Mapper.Sample.Models;
using FlowR.Mapper.Sample.Models.Domain;
using FlowR.Mapper.Sample.Models.Dto;
using FlowR.Mapper.Sample.Profiles;
using Microsoft.Extensions.DependencyInjection;

// ============================================================
// FlowR.Mapper Sample — demonstrates full API surface
// ============================================================

ServiceCollection services = new();

services.AddFlowRMapper(options =>
{
    options.ValidateOnStartup = false;
    options.AddProfile(new ECommerceProfile());
});

ServiceProvider provider = services.BuildServiceProvider();
IMapper mapper = provider.GetRequiredService<IMapper>();

Console.WriteLine("=== FlowR.Mapper Sample ===\n");

// ---- 1. Basic mapping ----
Console.WriteLine("--- 1. Basic Mapping ---");

UserEntity userEntity = new()
{
    Id = 1,
    FirstName = "Krish",
    LastName = "Dev",
    Email = "krish@flowr.dev",
    DateOfBirth = new DateTime(1990, 5, 15),
    IsActive = true,
    Address = new Address
    {
        Street = "123 Tech Lane",
        City = "Brisbane",
        PostCode = "4000"
    }
};

UserDto userDto = mapper.Map<UserEntity, UserDto>(userEntity);

Console.WriteLine($"Name: {userDto.FullName}");
Console.WriteLine($"Age: {userDto.Age}");
Console.WriteLine($"City: {userDto.Address?.City}");
Console.WriteLine($"Street: {userDto.Address?.Street}");
Console.WriteLine($"Address mapped type: {userDto.Address?.GetType().Name}\n");

// ---- 2. Collection mapping ----
Console.WriteLine("--- 2. Collection Mapping ---");

List<OrderEntity> orders =
[
    new() { OrderId = 1, Total = 150.00m, Status = "Shipped" },
    new() { OrderId = 2, Total = 89.99m, Status = "Pending" },
    new() { OrderId = 3, Total = 249.99m, Status = "Delivered" }
];

List<OrderDto> orderDtos = mapper.MapToList<OrderEntity, OrderDto>(orders);

orderDtos.ForEach(order =>
{
    Console.WriteLine($"  Order #{order.OrderId}: ${order.Total} [{order.Status}]");
});

Console.WriteLine();

// ---- 3. Flattened mapping ----
Console.WriteLine("--- 3. Flattened Mapping ---");

UserFlatDto flatDto = mapper.Map<UserEntity, UserFlatDto>(userEntity);

Console.WriteLine($"AddressCity: {flatDto.AddressCity}");
Console.WriteLine($"AddressPostCode: {flatDto.AddressPostCode}\n");

// ---- 4. Immutable record mapping ----
Console.WriteLine("--- 4. Immutable Record / ConstructUsing ---");

ProductEntity product = new()
{
    Id = 99,
    Name = "FlowR T-Shirt",
    Price = 29.99m,
    CategoryId = 5
};

ProductDto productDto = mapper.Map<ProductEntity, ProductDto>(product);

Console.WriteLine($"Product: {productDto.Name} | Price: ${productDto.Price} | Category: {productDto.Category}\n");

// ---- 5. Deep mapping ----
Console.WriteLine("--- 5. Deep Mapping ---");

UserEntity userWithOrders = new()
{
    Id = 2,
    FirstName = "Alice",
    LastName = "Smith",
    Email = "alice@flowr.dev",
    DateOfBirth = new DateTime(1985, 3, 10),
    IsActive = true,
    Address = new Address
    {
        City = "Wellington",
        Street = "99 Harbour Rd",
        PostCode = "6011"
    },
    Orders =
    [
        new OrderEntity
        {
            OrderId = 10,
            Total = 500m,
            Status = "Processing"
        }
    ]
};

UserDto fullDto = mapper.Map<UserEntity, UserDto>(userWithOrders);

Console.WriteLine($"{fullDto.FullName} has {fullDto.Orders.Count} order(s):");

fullDto.Orders.ForEach(order =>
{
    Console.WriteLine($"  Order #{order.OrderId}: ${order.Total}");
});

Console.WriteLine($"Nested address mapped type: {fullDto.Address?.GetType().Name}\n");

// ---- 6. Map into existing instance ----
Console.WriteLine("--- 6. Map Into Existing Instance ---");

UserDto existingDto = new()
{
    Id = 0,
    Email = "old@email.com"
};

mapper.Map(userEntity, existingDto);

Console.WriteLine($"Updated existing DTO Id: {existingDto.Id}, Email: {existingDto.Email}\n");

// ---- 7. HasMapping check ----
Console.WriteLine("--- 7. HasMapping ---");

Console.WriteLine($"UserEntity -> UserDto: {mapper.HasMapping<UserEntity, UserDto>()}");
Console.WriteLine($"UserEntity -> ProductDto: {mapper.HasMapping<UserEntity, ProductDto>()}");

Console.WriteLine("\n=== New Features ===\n");

// ---- 8. ForAllMembers null-safe update ----
Console.WriteLine("--- 8. ForAllMembers Null-Safe Update ---");

ServiceCollection servicesForAll = new();

servicesForAll.AddFlowRMapper(config =>
{
    config.CreateMap<UserEntity, UserDto>()
        .ForAllMembers(options =>
        {
            options.Condition((source, destination, sourceMember, destinationMember) => sourceMember != null);
        })
        .ForMember(destination => destination.FullName, options =>
            options.MapFrom(source => $"{source.FirstName} {source.LastName}"));
});

IMapper forAllMapper = servicesForAll.BuildServiceProvider().GetRequiredService<IMapper>();

UserDto existingForAllDto = new()
{
    Id = 99,
    Email = "keep-this@email.com",
    FullName = "Existing Name"
};

UserEntity nullUpdateUser = new()
{
    Id = 10,
    FirstName = "Updated",
    LastName = "User",
    Email = null
};

forAllMapper.Map(nullUpdateUser, existingForAllDto);

Console.WriteLine($"FullName updated: {existingForAllDto.FullName}");
Console.WriteLine($"Email preserved because source was null: {existingForAllDto.Email}\n");

// ---- 9. PreCondition ----
Console.WriteLine("--- 9. PreCondition ---");

ServiceCollection servicesPreCondition = new();

servicesPreCondition.AddFlowRMapper(config =>
{
    config.CreateMap<UserEntity, UserDto>()
        .PreCondition(source => source.IsActive)
        .ForMember(destination => destination.FullName, options =>
            options.MapFrom(source => $"{source.FirstName} {source.LastName}"));
});

IMapper preConditionMapper = servicesPreCondition.BuildServiceProvider().GetRequiredService<IMapper>();

UserEntity activeUser = new()
{
    FirstName = "Active",
    LastName = "User",
    IsActive = true
};

UserEntity inactiveUser = new()
{
    FirstName = "Inactive",
    LastName = "User",
    IsActive = false
};

UserDto activeResult = preConditionMapper.Map<UserEntity, UserDto>(activeUser);
UserDto? inactiveResult = preConditionMapper.Map<UserEntity, UserDto>(inactiveUser);

Console.WriteLine($"Active user mapped: {activeResult.FullName}");
Console.WriteLine($"Inactive user result: '{inactiveResult?.FullName ?? "[mapping skipped]"}'\n");

// ---- 10. AfterMap with ResolutionContext ----
Console.WriteLine("--- 10. AfterMap with ResolutionContext ---");

ServiceCollection servicesContext = new();

servicesContext.AddFlowRMapper(config =>
{
    config.CreateMap<Address, AddressDto>();
    config.CreateMap<OrderEntity, OrderDto>();

    config.CreateMap<UserEntity, UserDto>()
        .ForMember(destination => destination.FullName, options =>
            options.MapFrom(source => $"{source.FirstName} {source.LastName}"))
        .ForMember(destination => destination.Age, options =>
            options.MapFrom(source => DateTime.Today.Year - source.DateOfBirth.Year))
        .DeepMap()
        .AfterMap((source, destination, context) =>
        {
            destination.Email = $"processed_at_depth_{context.Depth}@test.com";
            Console.WriteLine($"  Context.Depth: {context.Depth}");
            Console.WriteLine($"  Context.SourceType: {context.SourceType.Name}");
        });
});

IMapper contextMapper = servicesContext.BuildServiceProvider().GetRequiredService<IMapper>();
UserDto contextDto = contextMapper.Map<UserEntity, UserDto>(userEntity);

Console.WriteLine($"Email after AfterMap: {contextDto.Email}\n");

// ---- 11. IMappingAction ----
Console.WriteLine("--- 11. IMappingAction ---");

ServiceCollection servicesAction = new();

servicesAction.AddFlowRMapper(config =>
{
    config.CreateMap<Address, AddressDto>();
    config.CreateMap<OrderEntity, OrderDto>();

    config.CreateMap<UserEntity, UserDto>()
        .ForMember(destination => destination.FullName, options =>
            options.MapFrom(source => $"{source.FirstName} {source.LastName}"))
        .ForMember(destination => destination.Age, options =>
            options.MapFrom(source => DateTime.Today.Year - source.DateOfBirth.Year))
        .DeepMap()
        .AfterMap(new AuditAction());
});

IMapper actionMapper = servicesAction.BuildServiceProvider().GetRequiredService<IMapper>();
UserDto auditDto = actionMapper.Map<UserEntity, UserDto>(userEntity);

Console.WriteLine($"Audited email: {auditDto.Email}\n");
// ---- 12. ConvertUsing ----
Console.WriteLine("--- 12. ConvertUsing ---");

ServiceCollection servicesConvert = new();

servicesConvert.AddFlowRMapper(config =>
{
    config.CreateMap<string, int>()
        .ConvertUsing(source => int.TryParse(source, out int result) ? result : 0);
});

IMapper convertMapper = servicesConvert.BuildServiceProvider().GetRequiredService<IMapper>();
int number = convertMapper.Map<string, int>("42");

Console.WriteLine($"String '42' converted to int: {number}\n");

Console.WriteLine("=== All Features Demonstrated ===");