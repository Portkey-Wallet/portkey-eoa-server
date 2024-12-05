using System.Threading.Tasks;
using Asp.Versioning;
using EoaServer.UserActivity.Dto;
using EoaServer.UserActivity.Dtos;
using EoaServer.UserActivity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace EoaServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserActivity")]
[Route("api/app/user/activities")]
// [Authorize]
public class UserActivityController : EoaServerBaseController
{
    private readonly IUserActivityAppService _userActivityAppService;

    public UserActivityController(IUserActivityAppService userActivityAppService)
    {
        _userActivityAppService = userActivityAppService;
    }
    
    [HttpPost("activities")]
    public async Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto requestDto)
    {
        return await _userActivityAppService.GetActivitiesAsync(requestDto);
    }

    [HttpPost("activity")]
    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto requestDto)
    {
        return await _userActivityAppService.GetActivityAsync(requestDto);
    }
}