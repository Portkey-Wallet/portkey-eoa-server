using System.Collections.Generic;
using System.Threading.Tasks;
using EoaServer.Token.Dto;
using EoaServer.Token.Request;

namespace EoaServer.Token;

public interface ITokenAppService
{
    Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input);
}