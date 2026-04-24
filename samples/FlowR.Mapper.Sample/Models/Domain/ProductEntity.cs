using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Sample.Models.Domain
{
    public class ProductEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int CategoryId { get; set; }
    }
}
