using FlowR.Mapper;
using FlowR.Mapper.Core;
using FlowR.Mapper.Tests.TestModels;

namespace FlowR.Mapper.Tests.Resolvers;

// ======================================================
// Value Resolvers for Testing
// ======================================================

/// <summary>
/// Resolves full name from first and last name.
/// </summary>
public class FullNameResolver : IValueResolver<EmployeeEntity, EmployeeDto, string>
{
    public string Resolve(EmployeeEntity source, EmployeeDto destination, string destMember, ResolutionContext context)
    {
        return $"{source.FirstName} {source.LastName}";
    }
}

/// <summary>
/// Formats salary as currency string.
/// </summary>
public class SalaryFormatter : IValueResolver<EmployeeEntity, EmployeeDto, string>
{
    public string Resolve(EmployeeEntity source, EmployeeDto destination, string destMember, ResolutionContext context)
    {
        return $"${source.Salary:N2}";
    }
}

/// <summary>
/// Generates department code from department name.
/// </summary>
public class DepartmentCodeResolver : IValueResolver<EmployeeEntity, EmployeeDto, string>
{
    public string Resolve(EmployeeEntity source, EmployeeDto destination, string destMember, ResolutionContext context)
    {
        return source.Department.Length >= 3
            ? source.Department.Substring(0, 3).ToUpper()
            : source.Department.ToUpper();
    }
}

/// <summary>
/// Resolver that uses context to access custom items.
/// </summary>
public class ContextAwareResolver : IValueResolver<EmployeeEntity, EmployeeDto, string>
{
    public string Resolve(EmployeeEntity source, EmployeeDto destination, string destMember, ResolutionContext context)
    {
        var prefix = context.Items.TryGetValue("Prefix", out var p) ? p.ToString() : "";
        return $"{prefix}{source.FirstName} {source.LastName}";
    }
}
