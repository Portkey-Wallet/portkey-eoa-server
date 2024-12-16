using System.Collections.Generic;
using System.Threading.Tasks;

namespace EoaServer.Token.TokenPrice;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string symbol);
    Task<Dictionary<string, decimal>> GetPriceAsync(params string[] symbols);
}