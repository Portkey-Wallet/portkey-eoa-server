using System.Threading.Tasks;
using EoaServer.Awaken;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dto;
using EoaServer.UserAssets.Dtos;

namespace EoaServer.UserAssets;

public interface IUserAssetsAppService
{
    Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto);
    Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto);
    Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto);
    Task<NftItem> GetNFTItemAsync(GetNftItemRequestDto requestDto);
    Task<AwakenSupportedTokenResponse> ListAwakenSupportedTokensAsync(int skipCount, int maxResultCount,
        int page, string chainId, string caAddress);
    Task<SearchUserAssetsV2Dto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto);
}