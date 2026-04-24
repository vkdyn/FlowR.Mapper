using FlowR.Mapper.Sample.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Sample.Models.Domain
{
    public class UserEntity
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public DateTime DateOfBirth { get; set; }

        public bool IsActive { get; set; }

        public Address? Address { get; set; }

        public List<OrderEntity> Orders { get; set; } = [];
    }
}
