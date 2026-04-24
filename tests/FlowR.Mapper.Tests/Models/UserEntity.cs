using FlowR.Mapper.Tests.Models;
using System;

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