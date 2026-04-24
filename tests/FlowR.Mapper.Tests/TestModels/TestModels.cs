using FlowR.Mapper.Tests.Models;

namespace FlowR.Mapper.Tests.TestModels;

// ======================================================
// Base Models for IncludeBase tests
// ======================================================

public class EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DtoBase
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DerivedEntity : EntityBase
{
    public string Name { get; set; } = "";
}

public class DerivedDto : DtoBase
{
    public string Name { get; set; } = "";
}

// ======================================================
// Customer Models for ResolutionContext tests
// ======================================================

public class CustomerEntity
{
    public string Name { get; set; } = "";
    public List<OrderEntity> Orders { get; set; } = [];
}

public class CustomerDto
{
    public string Name { get; set; } = "";
    public int OrderCount { get; set; }
    public int MappingDepth { get; set; }
    public string SourceTypeName { get; set; } = "";
    public string DestinationTypeName { get; set; } = "";
}

// ======================================================
// Product Models for immutable records
// ======================================================

public record ProductRecord(int Id, string Name, decimal Price);
public record ProductDto(int Id, string Name, decimal Price, string Category);

// ======================================================
// Person Models for ForPath tests
// ======================================================

public class PersonEntity
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public ContactInfo Contact { get; set; } = new();
}

public class ContactInfo
{
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public AddressInfo Address { get; set; } = new();
}

public class AddressInfo
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class PersonDto
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string City { get; set; } = "";
    public AddressInfo Address { get; set; } = new();
}

// ======================================================
// Employee Models for ValueResolver tests
// ======================================================

public class EmployeeEntity
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }
    public string Department { get; set; } = "";
}

public class EmployeeDto
{
    public string FullName { get; set; } = "";
    public string FormattedSalary { get; set; } = "";
    public string DepartmentCode { get; set; } = "";
}

// ======================================================
// Order Models for SetMappingOrder tests
// ======================================================

public class OrderProcessEntity
{
    public string OrderNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
}

public class OrderProcessDto
{
    public string ComputedField { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal Amount { get; set; }
    public string OrderNumber { get; set; } = "";
}

// ======================================================
// Account Models for UseDestinationValue tests
// ======================================================

public class AccountEntity
{
    public string AccountNumber { get; set; } = "";
    public decimal Balance { get; set; }
}

public class AccountDto
{
    public string AccountNumber { get; set; } = "";
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// ======================================================
// Config Models for ForAllOtherMembers tests
// ======================================================

public class ConfigSource
{
    public string Setting1 { get; set; } = "";
    public string Setting2 { get; set; } = "";
    public string Setting3 { get; set; } = "";
    public string SpecialSetting { get; set; } = "";
}

public class ConfigDestination
{
    public string Setting1 { get; set; } = "";
    public string Setting2 { get; set; } = "";
    public string Setting3 { get; set; } = "";
    public string SpecialSetting { get; set; } = "";
}
