using AutoMapper;
using EoaServer.Entities.Es;
using EoaServer.Grain.UserToken;
using EoaServer.Options;
using EoaServer.Token.Eto;

namespace EoaServer;

public class EoaServerApplicationAutoMapperProfile : Profile
{
    public EoaServerApplicationAutoMapperProfile()
    {
        //CreateMap<DeviceInfoDto, DeviceInfo>();
        CreateMap<UserTokenGrainDto, UserTokenEto>().ReverseMap();
        CreateMap<UserTokenItem, UserTokenIndex>();
        CreateMap<EoaServer.Options.Token, EoaServer.Entities.Es.Token>();
    }
}