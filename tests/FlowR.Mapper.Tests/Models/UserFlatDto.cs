using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Tests.Models
{
    public class UserFlatDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string AddressCity { get; set; } = "";
        public string AddressPostCode { get; set; } = "";
    }
}
