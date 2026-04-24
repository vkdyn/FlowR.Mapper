using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Sample.Models.Dto
{
    public class UserDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public int Age { get; set; }

        public AddressDto? Address { get; set; }

        public List<OrderDto> Orders { get; set; } = [];
    }
}
