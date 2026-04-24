using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Tests.Models
{
    public class OrderEntity
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "";
    }
}
