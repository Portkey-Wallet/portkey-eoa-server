using System.Threading.Tasks;
using EoaServer.UserActivity.Dto;
using EoaServer.UserActivity.Dtos;

namespace EoaServer.UserActivity;

public interface IUserActivityAppService
{
    Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request);
    Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request);
}