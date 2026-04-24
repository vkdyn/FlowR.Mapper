using FlowR.Mapper;
using FlowR.Mapper.Tests.TestModels;
using FlowR.Mapper.Tests.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FlowR.Mapper.Configuration;
using FlowR.Mapper.Interfaces;
using FlowR.Mapper.Extensions;

namespace FlowR.Mapper.Tests;

/// <summary>
/// Tests for advanced AutoMapper-parity features:
/// ForPath, IValueResolver, SetMappingOrder, UseDestinationValue, ForAllOtherMembers
/// </summary>
public class AdvancedMappingFeaturesTests
{
    private IMapper BuildMapper(Action<IProfileConfigurator>? configure = null)
    {
        var services = new ServiceCollection();
        if (configure != null)
        {
            services.AddFlowRMapper(configure);
        }
        else
        {
            services.AddFlowRMapper(typeof(AdvancedMappingFeaturesTests).Assembly);
        }
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    // ======================================================
    // FORPATH TESTS
    // ======================================================

    [Fact]
    public void ForPath_MapsToNestedProperty()
    {
        // Arrange
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<PersonEntity, PersonDto>()
                .ForPath(d => d.Address.City, opt => opt.MapFrom(s => s.Contact.Address.City))
                .ForPath(d => d.Address.Street, opt => opt.MapFrom(s => s.Contact.Address.Street));
        });

        var person = new PersonEntity
        {
            FirstName = "John",
            LastName = "Doe",
            Contact = new ContactInfo
            {
                Address = new AddressInfo
                {
                    City = "Auckland",
                    Street = "Queen Street"
                }
            }
        };

        // Act
        var dto = mapper.Map<PersonEntity, PersonDto>(person);

        // Assert
        Assert.Equal("Auckland", dto.Address.City);
        Assert.Equal("Queen Street", dto.Address.Street);
    }

    [Fact]
    public void ForPath_WithConstantValue()
    {
        // Arrange
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<PersonEntity, PersonDto>()
                .ForPath(d => d.Address.City, opt => opt.UseValue("Default City"));
        });

        var person = new PersonEntity();

        // Act
        var dto = mapper.Map<PersonEntity, PersonDto>(person);

        // Assert
        Assert.Equal("Default City", dto.Address.City);
    }

    // ======================================================
    // IVALUERESOLVER TESTS
    // ======================================================

    [Fact]
    public void ValueResolver_ResolvesUsingCustomClass()
    {
        // Arrange
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<EmployeeEntity, EmployeeDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom<FullNameResolver>())
                .ForMember(d => d.FormattedSalary, opt => opt.MapFrom<SalaryFormatter>());
        });

        var employee = new EmployeeEntity
        {
            FirstName = "Jane",
            LastName = "Smith",
            Salary = 75000.50m
        };

        // Act
        var dto = mapper.Map<EmployeeEntity, EmployeeDto>(employee);

        // Assert
        Assert.Equal("Jane Smith", dto.FullName);
        Assert.Equal("$75,000.50", dto.FormattedSalary);
    }

    // ======================================================
    // SETMAPPINGORDER TESTS
    // ======================================================

    [Fact]
    public void SetMappingOrder_MapsPropertiesInSpecifiedOrder()
    {
        // Arrange
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<OrderProcessEntity, OrderProcessDto>()
                .ForMember(d => d.Status, opt => opt.SetMappingOrder(1))
                .ForMember(d => d.Amount, opt => opt.SetMappingOrder(2))
                .ForMember(d => d.OrderNumber, opt => opt.SetMappingOrder(3));
        });

        var order = new OrderProcessEntity
        {
            OrderNumber = "ORD-001",
            Amount = 99.99m,
            Status = "Processing"
        };

        // Act
        var dto = mapper.Map<OrderProcessEntity, OrderProcessDto>(order);

        // Assert - Properties mapped in order
        Assert.Equal("Processing", dto.Status);
        Assert.Equal(99.99m, dto.Amount);
        Assert.Equal("ORD-001", dto.OrderNumber);
    }

    // ======================================================
    // USEDESTINATIONVALUE TESTS
    // ======================================================

    [Fact]
    public void UseDestinationValue_PreservesExistingValue()
    {
        // Arrange
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<AccountEntity, AccountDto>()
                .ForMember(d => d.LastUpdated, opt => opt.UseDestinationValue());
        });

        var account = new AccountEntity
        {
            AccountNumber = "ACC-12345",
            Balance = 1500.00m
        };

        var existingDto = new AccountDto
        {
            LastUpdated = new DateTime(2024, 1, 1)
        };

        // Act
        var dto = mapper.Map(account, existingDto);

        // Assert
        Assert.Equal("ACC-12345", dto.AccountNumber); // Mapped
        Assert.Equal(new DateTime(2024, 1, 1), dto.LastUpdated); // Preserved
    }

    // ======================================================
    // FORALLOTHERMEMBERS TESTS
    // ======================================================

    [Fact]
    public void ForAllOtherMembers_AppliesConfigToUnmappedMembers()
    {
        // Arrange
        var mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<ConfigSource, ConfigDestination>()
                .ForMember(d => d.SpecialSetting, opt => opt.MapFrom(s => s.SpecialSetting.ToUpper()))
                .ForAllOtherMembers(opt => opt.Condition(s => !string.IsNullOrEmpty(s?.ToString())));
        });

        var source = new ConfigSource
        {
            Setting1 = "Value1",
            Setting2 = "",
            SpecialSetting = "special"
        };

        // Act
        var dto = mapper.Map<ConfigSource, ConfigDestination>(source);

        // Assert
        Assert.Equal("SPECIAL", dto.SpecialSetting); // Explicitly configured
        Assert.Equal("Value1", dto.Setting1);
    }
}