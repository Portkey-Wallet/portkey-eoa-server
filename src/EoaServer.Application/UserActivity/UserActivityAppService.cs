using System.Threading.Tasks;
using EoaServer.UserActivity.Dto;
using EoaServer.UserActivity.Dtos;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace EoaServer.UserActivity;

[RemoteService(false)]
[DisableAuditing]
public class UserActivityAppService : EoaServerBaseService, IUserActivityAppService
{
    public async Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request)
    {
        //todo
        return null;
    }

    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request)
    {
        //todo
        return null;
    }
}