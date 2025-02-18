using System;
using System.Threading.Tasks;
using Asp.Versioning;
using EoaServer.Awaken;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace EoaServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserAssets")]
[Route("api/app/user/assets")]
// [Authorize]
public class UserAssetsController : EoaServerBaseController
{
    private readonly IUserAssetsAppService _userAssetsAppService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserAssetsController(IUserAssetsAppService userAssetsAppService, IHttpContextAccessor httpContextAccessor)
    {
        _userAssetsAppService = userAssetsAppService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("token")]
    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        return await _userAssetsAppService.GetTokenAsync(requestDto);
    }
    
    [HttpPost("nftCollections")]
    public async Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto)
    {
        return await _userAssetsAppService.GetNFTCollectionsAsync(requestDto);
    }

    [HttpPost("nftItems")]
    public async Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        return await _userAssetsAppService.GetNFTItemsAsync(requestDto);
    }
    
    [HttpGet("awaken/token")]
    public async Task<AwakenSupportedTokenResponse> ListAwakenSupportedTokensAsync(int skipCount, int maxResultCount,
        int page, string chainId, string address)
    {
        skipCount = skipCount <= 0 ? 0 : skipCount;
        maxResultCount = maxResultCount <= 0 ? 100 : maxResultCount;
        page = page <= 1 ? 1 : page;
        return await _userAssetsAppService.ListAwakenSupportedTokensAsync(skipCount, maxResultCount, page, chainId, address);
    }
}