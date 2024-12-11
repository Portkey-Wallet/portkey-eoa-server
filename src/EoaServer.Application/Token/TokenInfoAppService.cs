using System;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token.Dto;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;

namespace EoaServer.Token;

[RemoteService(false)]
[DisableAuditing]
public class TokenInfoAppService : EoaServerBaseService, ITokenInfoAppService
{
    private readonly IDistributedCache<TokenInfoDto> _tokenInfoCache;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly AElfScanOptions _aElfScanOptions;
    private readonly TokenInfoOptions _tokenInfoOptions;
    private readonly AssetsInfoOptions _assetsInfoOptions;
    private readonly ILogger<TokenInfoAppService> _logger;

    public TokenInfoAppService(IDistributedCache<TokenInfoDto> tokenCache,
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<AElfScanOptions> aElfScanOptions,
        IOptionsSnapshot<TokenInfoOptions> tokenInfoOptions,
        IOptionsSnapshot<AssetsInfoOptions> assetsInfoOptions,
        ILogger<TokenInfoAppService> logger)
    {
        _tokenInfoCache = tokenCache;
        _httpClientProvider = httpClientProvider;
        _aElfScanOptions = aElfScanOptions.Value;
        _tokenInfoOptions = tokenInfoOptions.Value;
        _assetsInfoOptions = assetsInfoOptions.Value;
        _logger = logger;
    }

    private string GetTokenImage(string symbol)
    {
        if (symbol.IsNullOrWhiteSpace() || _tokenInfoOptions?.TokenInfos == null)
        {
            return string.Empty;
        }
        
        if (_tokenInfoOptions.TokenInfos.ContainsKey(symbol))
        {
            return _tokenInfoOptions.TokenInfos[symbol].ImageUrl;
        }

        if (_assetsInfoOptions.ImageUrlPrefix.IsNullOrWhiteSpace() || _assetsInfoOptions.ImageUrlSuffix.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        return $"{_assetsInfoOptions.ImageUrlPrefix}{symbol}{_assetsInfoOptions.ImageUrlSuffix}";
    }

    public async Task<IndexerTokenInfoDto> GetIndexerTokenInfoAsync(string chainId, string symbol)
    {
        var url = _aElfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTokenInfoApi;
        var requestUrl = $"{url}?Symbol={symbol}&ChainId={chainId}";
        var tokenInfoResult = await _httpClientProvider.GetDataAsync<IndexerTokenInfoDto>(requestUrl);
        if (tokenInfoResult == null)
        {
            _logger.LogError($"Token info result is null. Symbol: {symbol}, request: {requestUrl}");
        }
        return tokenInfoResult;
    }
    
    public async Task<TokenInfoDto> GetAsync(string chainId, string symbol)
    {
        var tokenKey = $"{CommonConstant.TokenInfoCachePrefix}:{symbol}:{chainId}";
        var tokenInfo = await _tokenInfoCache.GetAsync(tokenKey);
        if (tokenInfo != null)
        {
            return tokenInfo;
        }
        
        var url = _aElfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTokenInfoApi;
        var requestUrl = $"{url}?Symbol={symbol}&ChainId={chainId}";
        var tokenInfoResult = await _httpClientProvider.GetDataAsync<IndexerTokenInfoDto>(requestUrl);

        if (tokenInfoResult == null)
        {
            return null;
        }
        
        tokenInfo = new TokenInfoDto
        {
            Symbol = tokenInfoResult.Symbol,
            Decimals = tokenInfoResult.Decimals,
            ChainId = tokenInfoResult.IssueChainId,
            ImageUri = GetTokenImage(tokenInfoResult.Symbol),
            TokenName = tokenInfoResult.TokenName
        };

        _tokenInfoCache.SetAsync(tokenKey, tokenInfo, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
        });
        return tokenInfo;
    }
}