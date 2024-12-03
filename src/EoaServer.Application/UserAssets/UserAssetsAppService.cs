using System.Threading.Tasks;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace EoaServer.UserAssets;


[RemoteService(false)]
[DisableAuditing]
public class UserAssetsAppService : EoaServerBaseService, IUserAssetsAppService
{
    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        //todo
        return null;
    }
    
    public async Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto)
    {
        //todo
        return null;
    }

    public async Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        //todo
        return null;
    }
}