using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token.Dto;
using EoaServer.UserActivity.Dto;
using EoaServer.UserAssets.Dtos;
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
    
    [ExceptionHandler(typeof(Exception), Message = "GetTokenListAsync Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
    public async Task<ListResponseDto<TokenCommonDto>> GetTokenListAsync(string chainId, string search)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTokenListApi;
        var requestUrl = $"{url}?chainId={chainId}&fuzzySearch={search.ToLower()}&skipCount=0&maxResultCount={LimitedResultRequestDto.MaxMaxResultCount}";
        var chainTokenList = await _httpClientProvider.GetDataAsync<ListResponseDto<TokenCommonDto>>(requestUrl, _aelfScanOptions.Timeout);
        if (chainTokenList == null)
        {
            _logger.LogError($"Http request: {requestUrl} get null response");
        }
        return chainTokenList;
    }

    [ExceptionHandler(typeof(Exception), Message = "GetAddressTokenAssetsAsync Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
    public async Task<GetAddressTokenListResultDto> GetAddressTokenAssetsAsync(string chainId, string address)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanUserTokenAssetsApi;
        var requestUrl = $"{url}?address={address}&chainId={chainId}&skipCount=0&MaxResultCount={LimitedResultRequestDto.MaxMaxResultCount}";
        var chainTokenList = await _httpClientProvider.GetDataAsync<GetAddressTokenListResultDto>(requestUrl, _aelfScanOptions.Timeout);
        if (chainTokenList == null)
        {
            _logger.LogError($"Http request: {requestUrl} get null response");
        }
        return chainTokenList;
    }

    [ExceptionHandler(typeof(Exception), Message = "GetAddressNftListAsync Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
    public async Task<GetAddressNftListResultDto> GetAddressNftListAsync(string chainId, string address)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanUserNFTAssetsApi;
        var requestUrl = $"{url}?address={address}&chainId={chainId}&skipCount=0&maxResultCount={LimitedResultRequestDto.MaxMaxResultCount}";
        var chainTokenList = await _httpClientProvider.GetDataAsync<GetAddressNftListResultDto>(requestUrl, _aelfScanOptions.Timeout);
        if (chainTokenList == null)
        {
            _logger.LogError($"Http request: {requestUrl} get null response");
        }
        return chainTokenList;
    }

    public async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(string chainId, string transactionId)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTransactionDetailApi;
        var requestUrl = $"{url}?TransactionId={transactionId}&ChainId={chainId}";
        
        try
        {
            var txnDto = await _httpClientProvider.GetDataAsync<TransactionDetailResponseDto>(requestUrl, _aelfScanOptions.Timeout);
            return txnDto;
        }
        catch (Exception e)
        {
            _logger.LogError($"GetTransactionDetailAsync error. Request: {requestUrl}. Exception: {e}");
        }

        return null;
    }
    
    public async Task<TransactionsResponseDto> GetAddressTransactionsAsync(string chainId, string address, int skipCount, int maxResultCount)
    {
        var baseUrl = _aelfScanOptions.BaseUrl;

        var transactionsUrl = $"{baseUrl}/{CommonConstant.AelfScanUserTransactionsApi}?";
        transactionsUrl += chainId != null ? $"chainId={chainId}&" : "";
        transactionsUrl += $"address={address}&" +
                           $"skipCount={skipCount}&" +
                           $"maxResultCount={maxResultCount}";
        try
        {
            return await _httpClientProvider.GetDataAsync<TransactionsResponseDto>(transactionsUrl, _aelfScanOptions.Timeout);
        }
        catch (Exception e)
        {
            _logger.LogError($"Http request: {transactionsUrl} get null response");
        }
        return null;
    }
    
    [ExceptionHandler(typeof(Exception), Message = "GetAddressTransfersAsync Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
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
        var response = await _httpClientProvider.GetDataAsync<GetTransferListResultDto>(nftTransfersUrl, _aelfScanOptions.Timeout);
        return response;
    }
    
    [ExceptionHandler(typeof(Exception), Message = "GetIndexerTokenInfoAsync Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
    public async Task<IndexerTokenInfoDto> GetIndexerTokenInfoAsync(string chainId, string symbol)
    {
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTokenInfoApi;
        var requestUrl = $"{url}?Symbol={symbol}&ChainId={chainId}";
        var tokenInfoResult = await _httpClientProvider.GetDataAsync<IndexerTokenInfoDto>(requestUrl, _aelfScanOptions.Timeout);
        if (tokenInfoResult == null)
        {
            _logger.LogError($"Http request: {requestUrl} get null response");
        }
        return tokenInfoResult;
    }
    
}