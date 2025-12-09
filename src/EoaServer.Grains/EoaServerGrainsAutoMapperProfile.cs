using AutoMapper;
using EoaServer.Grain.Test;
using EoaServer.Grain.UserToken;
using EoaServer.State.Test;
using EoaServer.State.UserToken;

namespace EoaServer;

public class EoaServerGrainsAutoMapperProfile : Profile
{
    public EoaServerGrainsAutoMapperProfile()
    {
        CreateMap<TestState, TestGrainDto>().ReverseMap();
        CreateMap<UserTokenGrainDto, UserTokenState>().ReverseMap();
        CreateMap<EoaServer.UserToken.Dto.Token, EoaServer.State.UserToken.Token>().ReverseMap();
        
    }
}