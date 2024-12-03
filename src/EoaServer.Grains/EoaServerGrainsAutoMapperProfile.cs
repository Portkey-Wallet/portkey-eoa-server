using AutoMapper;
using EoaServer.Grain.Test;
using EoaServer.State.Test;

namespace EoaServer;

public class EoaServerGrainsAutoMapperProfile : Profile
{
    public EoaServerGrainsAutoMapperProfile()
    {
        CreateMap<TestState, TestGrainDto>().ReverseMap();
    }
}