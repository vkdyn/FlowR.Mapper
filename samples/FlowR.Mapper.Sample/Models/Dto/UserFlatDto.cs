using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Sample.Models.Dto
{
    public class UserFlatDto
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string AddressCity { get; set; } = string.Empty;

        public string AddressPostCode { get; set; } = string.Empty;
    }
}
