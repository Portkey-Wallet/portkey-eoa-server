using System.Threading.Tasks;
using EoaServer.Commons;
using EoaServer.Token.Dto;
using EoaServer.UserActivity.Dto;
using EoaServer.UserAssets.Dtos;

namespace EoaServer.Common;

public interface IAElfScanDataProvider
{
    Task<ListResponseDto<TokenCommonDto>> GetTokenListAsync(string chainId, string search);
    Task<GetAddressTokenListResultDto> GetAddressTokenAssetsAsync(string chainId, string address);
    Task<GetAddressNftListResultDto> GetAddressNftListAsync(string chainId, string address);
    Task<TransactionDetailResponseDto> GetTransactionDetailAsync(string chainId, string transactionId);
    Task<TransactionsResponseDto> GetAddressTransactionsAsync(string chainId, string address, int skipCount, int maxResultCount);
    Task<GetTransferListResultDto> GetAddressTransfersAsync(string chainId, string address, int tokenType,
        int skipCount, int maxResultCount, string symbol);
    Task<IndexerTokenInfoDto> GetIndexerTokenInfoAsync(string chainId, string symbol);
}