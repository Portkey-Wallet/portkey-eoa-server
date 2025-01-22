using System.Threading.Tasks;
using Asp.Versioning;
using EoaServer.Search;
using EoaServer.Search.Dto;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace EoaServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Search")]
[Route("api/app/search")]
public class SearchController : EoaServerBaseController
{
    private readonly ISearchAppService _searchAppService;

    public SearchController(ISearchAppService searchAppService)
    {
        _searchAppService = searchAppService;
    }

    [HttpGet("chainsinfoindex")]
    public async Task<PagedResultDto<ChainsInfoDto>> GetChainsInfoAsync()
    {
        return await _searchAppService.GetChainsInfoAsync();
    }
}
