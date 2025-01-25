using System.Threading.Tasks;
using EoaServer.UserToken.Dto;
using EoaServer.UserToken.Request;
using Volo.Abp.Application.Dtos;

namespace EoaServer.UserToken;

public interface IUserTokenAppService
{
    Task<PagedResultDto<GetUserTokenDto>> GetTokensAsync(GetTokenInfosRequestDto requestDto);
    Task ChangeTokenDisplayAsync(string id, bool isDisplay);
}