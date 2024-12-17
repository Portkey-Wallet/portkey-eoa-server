using System.Collections.Generic;
using System.Threading.Tasks;
using EoaServer.Token.Dto;
using EoaServer.Token.Request;
using Volo.Abp.Application.Dtos;

namespace EoaServer.Token;

public interface ITokenAppService
{
    Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input);
    Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols);
}