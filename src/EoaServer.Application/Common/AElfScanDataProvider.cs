using System.Threading.Tasks;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token;
using EoaServer.Token.Dto;
using EoaServer.UserActivity.Dto;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using EoaServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace EoaServer.Common;

public class AElfScanDataProvider : IAElfScanDataProvider, ISingletonDependency
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly AElfScanOptions _aelfScanOptions;
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<AElfScanDataProvider> _logger;

    public AElfScanDataProvider(IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<AElfScanOptions> aElfScanOptions,
        IOptionsSnapshot<ChainOptions> chainOptions,
        ILogger<AElfScanDataProvider> logger)
    {
        _httpClientProvider = httpClientProvider;
        _aelfScanOptions = aElfScanOptions.Value;
        _chainOptions = chainOptions.Value;
        _logger = logger;
    }
    
    public async Task<ListResponseDto<TokenCommonDto>> GetTokenListAsync(string chainId, string search)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTokenListApi;
        var requestUrl = $"{url}?chainId={chainId}&fuzzySearch={search.ToLower()}&skipCount=0&maxResultCount={LimitedResultRequestDto.MaxMaxResultCount}";
        var chainTokenList = await _httpClientProvider.GetDataAsync<ListResponseDto<TokenCommonDto>>(requestUrl);
        return chainTokenList;
    }

    public async Task<GetAddressTokenListResultDto> GetAddressTokenAssetsAsync(string chainId, string address)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanUserTokenAssetsApi;
        var requestUrl = $"{url}?address={address}&chainId={chainId}&skipCount=0&MaxResultCount={LimitedResultRequestDto.MaxMaxResultCount}";
        var chainTokenList = await _httpClientProvider.GetDataAsync<GetAddressTokenListResultDto>(requestUrl);
        return chainTokenList;
    }

    public async Task<GetAddressNftListResultDto> GetAddressNftListAsync(string chainId, string address)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanUserNFTAssetsApi;
        var requestUrl = $"{url}?address={address}&chainId={chainId}&skipCount=0&maxResultCount={LimitedResultRequestDto.MaxMaxResultCount}";
        var chainTokenList = await _httpClientProvider.GetDataAsync<GetAddressNftListResultDto>(requestUrl);
        return chainTokenList;
    }

    public async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(string chainId, string transactionId)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTransactionDetailApi;
        var requestUrl = $"{url}?TransactionId={transactionId}&ChainId={chainId}";
        var txnDto = await _httpClientProvider.GetDataAsync<TransactionDetailResponseDto>(requestUrl);
        return txnDto;
    }
    
    public async Task<TransactionsResponseDto> GetAddressTransactionsAsync(string chainId, string address, int skipCount, int maxResultCount)
    {
        var baseUrl = _aelfScanOptions.BaseUrl;

        var transactionsUrl = $"{baseUrl}/{CommonConstant.AelfScanUserTransactionsApi}?";
        transactionsUrl += chainId != null ? $"chainId={chainId}&" : "";
        transactionsUrl += $"address={address}&" +
                           $"skipCount={skipCount}&" +
                           $"maxResultCount={maxResultCount}";
        var response = await _httpClientProvider.GetDataAsync<TransactionsResponseDto>(transactionsUrl);
        return response;
    }
    
    public async Task<GetTransferListResultDto> GetAddressTransfersAsync(string chainId, string address, int tokenType,
        int skipCount, int maxResultCount, string symbol)
    {
        var baseUrl = _aelfScanOptions.BaseUrl;
        var nftTransfersUrl = $"{baseUrl}/{CommonConstant.AelfScanUserTransfersApi}?";
        nftTransfersUrl += !string.IsNullOrEmpty(chainId) ? $"chainId={chainId}&" : "";
        nftTransfersUrl += !string.IsNullOrEmpty(symbol) ? $"symbol={symbol}&" : "";
        nftTransfersUrl += $"tokenType={tokenType}&" +
                           $"address={address}&" +
                           $"skipCount={skipCount}&" +
                           $"maxResultCount={maxResultCount}";
        return await _httpClientProvider.GetDataAsync<GetTransferListResultDto>(nftTransfersUrl);
    }
    
    public async Task<IndexerTokenInfoDto> GetIndexerTokenInfoAsync(string chainId, string symbol)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTokenInfoApi;
        var requestUrl = $"{url}?Symbol={symbol}&ChainId={chainId}";
        var tokenInfoResult = await _httpClientProvider.GetDataAsync<IndexerTokenInfoDto>(requestUrl);
        if (tokenInfoResult == null)
        {
            _logger.LogError($"{requestUrl} get null response");
        }
        return tokenInfoResult;
    }
    
}