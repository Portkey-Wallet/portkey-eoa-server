using System.Collections.Generic;
using Asp.Versioning;
using EoaServer.Commons;
using EoaServer.Token.Dto;
using EoaServer.Token.Request;
using EoaServer.UserToken;
using EoaServer.UserToken.Dto;
using EoaServer.UserToken.Request;

namespace EoaServer.Controllers;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;


[RemoteService]
[Area("app")]
[ControllerName("UserToken")]
[Route("api/app/userTokens")]
public class UserTokenController : EoaServerBaseController
{
    private readonly IUserTokenAppService _userTokenAppService;

    public UserTokenController(IUserTokenAppService userTokenService)
    {
        _userTokenAppService = userTokenService;
    }

    [HttpPut]
    [Route("{id}/display")]
    [Authorize]
    public async Task ChangeTokenDisplayAsync(string id, IsTokenDisplayInput input)
    {
        await _userTokenAppService.ChangeTokenDisplayAsync(id, input.IsDisplay);
    }

    [HttpGet, Authorize]
    public async Task<PagedResultDto<GetUserTokenDto>> GetTokensAsync(GetTokenInfosRequestDto requestDto)
    {
        return await _userTokenAppService.GetTokensAsync(requestDto);
    }
}