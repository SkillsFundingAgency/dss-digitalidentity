using AutoMapper;
using NCS.DSS.DigitalIdentity.DTO;

namespace NCS.DSS.DigitalIdentity.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<DigitalIdentityPatch, Models.DigitalIdentity>();
            CreateMap<DigitalIdentityPost, Models.DigitalIdentity>();
            CreateMap<Models.DigitalIdentity, DigitalIdentityPatch>();
            CreateMap<Models.DigitalIdentity, DigitalIdentityPost>();
        }
    }
}
