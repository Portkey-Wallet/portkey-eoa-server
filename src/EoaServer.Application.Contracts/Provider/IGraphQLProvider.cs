using System.Threading.Tasks;
using EoaServer.Provider.Dto.Indexer;

namespace EoaServer.Provider;

public interface IGraphQLProvider
{
    Task<IndexerTokenTransferListDto> GetTokenTransferInfoAsync(GetTokenTransferRequestDto requestDto);
    Task<IndexerTransactionListResultDto> GetTransactionsAsync(TransactionsRequestDto input);
}