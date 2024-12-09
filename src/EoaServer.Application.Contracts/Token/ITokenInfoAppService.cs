using System.Threading.Tasks;
using EoaServer.Token.Dto;

namespace EoaServer.Token;

public interface ITokenInfoAppService
{
    Task<TokenInfoDto> GetAsync(string chainId, string symbol);
    Task<IndexerTokenInfoDto> GetIndexerTokenInfoAsync(string chainId, string symbol);
}