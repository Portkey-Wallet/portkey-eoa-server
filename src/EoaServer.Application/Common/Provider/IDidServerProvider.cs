using System.Threading.Tasks;
using EoaServer.UserAssets.Dtos;

namespace EoaServer.Common.Provider;

public interface IDidServerProvider
{
    Task<NftItem> SetTraitsPercentAsync(string traits);
}