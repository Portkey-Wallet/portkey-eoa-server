using EoaServer.Token;

namespace EoaServer.Controllers;

using System.Collections.Generic;
using Asp.Versioning;
using EoaServer.Commons;
using EoaServer.Token.Dto;
using EoaServer.Token.Request;
using EoaServer.UserToken;
using EoaServer.UserToken.Dto;
using EoaServer.UserToken.Request;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;


[RemoteService]
[Area("app")]
[ControllerName("Token")]
[Route("api/app/tokens")]
public class TokenController : EoaServerBaseController
{
    private readonly ITokenAppService _tokenAppService;

    public TokenController(ITokenAppService tokenService)
    {
        _tokenAppService = tokenService;
    }
    
    [/*Authorize, */HttpGet("list")]
    public async Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input)
    {
        return await _tokenAppService.GetTokenListAsync(input);
    }
}