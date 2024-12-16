using AutoMapper;
using EoaServer.Entities.Es;
using EoaServer.Grain.UserToken;
using EoaServer.Token.Eto;

namespace EoaServer;

public class EoaServerApplicationAutoMapperProfile : Profile
{
    public EoaServerApplicationAutoMapperProfile()
    {
        //CreateMap<DeviceInfoDto, DeviceInfo>();
        CreateMap<UserTokenGrainDto, UserTokenEto>().ReverseMap();
    }
}