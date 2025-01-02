using System.Threading.Tasks;
using EoaServer.Provider.Dto.Indexer;

namespace EoaServer.Provider;

public interface IGraphQLProvider
{
    public Task<IndexerTokenTransferListDto> GetTokenTransferInfoAsync(TokenTransferInput input);
}