using System.Threading.Tasks;
using EoaServer.Search.Dto;
using Volo.Abp.Application.Dtos;

namespace EoaServer.Search;

public interface ISearchAppService
{
    Task<PagedResultDto<ChainsInfoDto>> GetChainsInfoAsync();
}