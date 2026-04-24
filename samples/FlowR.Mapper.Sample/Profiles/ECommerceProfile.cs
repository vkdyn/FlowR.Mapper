using FlowR.Mapper;
using FlowR.Mapper.Configuration;
using FlowR.Mapper.Sample.Models.Domain;
using FlowR.Mapper.Sample.Models.Dto;

namespace FlowR.Mapper.Sample.Profiles;

public class ECommerceProfile : MapperProfile
{
    private static readonly Dictionary<int, string> CategoryNames = new()
    {
        [1] = "Electronics",
        [2] = "Clothing",
        [3] = "Books",
        [4] = "Food",
        [5] = "Accessories"
    };

    public override void Configure(IProfileConfigurator cfg)
    {
        cfg.CreateMap<Address, AddressDto>();
        cfg.CreateMap<OrderEntity, OrderDto>();

        cfg.CreateMap<UserEntity, UserDto>()
            .ForMember(destination => destination.FullName, options =>
                options.MapFrom(source => $"{source.FirstName} {source.LastName}"))
            .ForMember(destination => destination.Age, options =>
                options.MapFrom(source => DateTime.Today.Year - source.DateOfBirth.Year))
            .DeepMap();

        cfg.CreateMap<UserEntity, UserFlatDto>()
            .Flatten();

        cfg.CreateMap<ProductEntity, ProductDto>(source =>
            new ProductDto(
                source.Id,
                source.Name,
                source.Price,
                CategoryNames.GetValueOrDefault(source.CategoryId, "Unknown")));

        cfg.AddValueTransform<string>(value => value?.Trim() ?? value!);
    }
}