using AutoMapper;
using EoaServer.Awaken;
using EoaServer.Commons;
using EoaServer.Entities.Es;
using EoaServer.Grain.UserToken;
using EoaServer.Options;
using EoaServer.Search.Dto;
using EoaServer.Token.Eto;
using EoaServer.Transfer.Dtos;
using EoaServer.UserAssets.Dto;
using EoaServer.UserAssets.Dtos;

namespace EoaServer;

public class EoaServerApplicationAutoMapperProfile : Profile
{
    public EoaServerApplicationAutoMapperProfile()
    {
        //CreateMap<DeviceInfoDto, DeviceInfo>();
        CreateMap<UserTokenGrainDto, UserTokenEto>().ReverseMap();
        CreateMap<UserTokenItem, UserTokenIndex>();
        CreateMap<EoaServer.Options.Token, EoaServer.Entities.Es.Token>();
        CreateMap<ChainsInfoIndex, ChainsInfoDto>();
        CreateMap<DefaultTokenInfo, DefaultTokenInfoDto>();
        CreateMap<TradePairsItemToken, UserAssets.Dtos.Token>();
        CreateMap<UserAssets.Dtos.Token, TokenInfoV2Dto>();
        CreateMap<NftItem, NftInfoDto>();
        CreateMap<AuthTokenRequestDto, ETransferAuthTokenRequestDto>().ForMember(des => des.ClientId,
                opt => opt.MapFrom(f => ETransferConstant.ClientId))
            .ForMember(des => des.GrantType,
                opt => opt.MapFrom(f => ETransferConstant.GrantType))
            .ForMember(des => des.Version,
                opt => opt.MapFrom(f => ETransferConstant.Version))
            .ForMember(des => des.Source,
                opt => opt.MapFrom(f => ETransferConstant.Source))
            .ForMember(des => des.Scope,
                opt => opt.MapFrom(f => ETransferConstant.Scope))
            ;
    }
}