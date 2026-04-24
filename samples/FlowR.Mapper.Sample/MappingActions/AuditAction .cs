using FlowR.Mapper.Sample.Models.Domain;
using FlowR.Mapper.Sample.Models.Dto;

namespace FlowR.Mapper.Sample.MappingActions
{

    public class AuditAction : IMappingAction<UserEntity, UserDto>
    {
        public void Process(UserEntity source, UserDto destination, ResolutionContext context)
        {
            destination.Email = $"audited_{source.Email}";
        }
    }
}
