using System.Threading.Tasks;
using EoaServer.Token.Dto;

namespace EoaServer.Token.TokenPrice;

public interface ITokenPriceService
{
    /// <summary>
    /// Retrieve token price from Redis cache
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    Task<TokenPriceDataDto> GetCurrentPriceAsync(string symbol);

    /// <summary>
    /// Refresh the token price in the Redis cache.
    /// </summary>
    /// <param name="symbol">Symbol == null, refresh all tokens.</param>
    /// <returns></returns>
    Task RefreshCurrentPriceAsync(string symbol = default);
}