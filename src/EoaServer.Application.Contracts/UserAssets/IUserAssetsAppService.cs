using System.Threading.Tasks;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;

namespace EoaServer.UserAssets;

public interface IUserAssetsAppService
{
    Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto);
    Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto);
    Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto);
}