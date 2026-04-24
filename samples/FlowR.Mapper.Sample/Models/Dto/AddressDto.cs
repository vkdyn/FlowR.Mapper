using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Sample.Models.Dto
{
    public class AddressDto
    {
        public string Street { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string PostCode { get; set; } = string.Empty;
    }
}
