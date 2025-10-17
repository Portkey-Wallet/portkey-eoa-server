using AutoMapper;
using EoaServer.Entities.Es;
using EoaServer.Token.Eto;

namespace EoaServer.EntityEventHandler.Core;

public class EoaServerEventHandlerAutoMapperProfile : Profile
{
    public EoaServerEventHandlerAutoMapperProfile()
    {
        CreateMap<UserTokenEto, UserTokenIndex>();
        CreateMap<EoaServer.UserToken.Dto.Token, EoaServer.Entities.Es.Token>();
    }
}