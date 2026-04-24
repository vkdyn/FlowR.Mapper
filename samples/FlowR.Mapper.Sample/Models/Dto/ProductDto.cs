using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Sample.Models.Dto
{
    public record ProductDto(
        int Id,
        string Name,
        decimal Price,
        string Category);
}
