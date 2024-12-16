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
using Volo.Abp.DependencyInjection;

namespace EoaServer.Token;


public interface ITokenInfoProvider
{
    Task<TokenInfoDto> GetAsync(string chainId, string symbol);
    string BuildSymbolImageUrl(string symbol);
    string GetTokenId(string chainId, string symbol);
}

public class TokenInfoProvider : ITokenInfoProvider, ISingletonDependency
{
    private readonly IDistributedCache<TokenInfoDto> _tokenInfoCache;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly AElfScanOptions _aelfScanOptions;
    private readonly TokenInfoOptions _tokenInfoOptions;
    private readonly AssetsInfoOptions _assetsInfoOptions;
    private readonly ILogger<TokenInfoProvider> _logger;
    private readonly ChainOptions _chainOptions;

    public TokenInfoProvider(IDistributedCache<TokenInfoDto> tokenCache,
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<AElfScanOptions> aElfScanOptions,
        IOptionsSnapshot<TokenInfoOptions> tokenInfoOptions,
        IOptionsSnapshot<AssetsInfoOptions> assetsInfoOptions,
        ILogger<TokenInfoProvider> logger,
        IOptionsSnapshot<ChainOptions> chainOptions)
    {
        _tokenInfoCache = tokenCache;
        _httpClientProvider = httpClientProvider;
        _aelfScanOptions = aElfScanOptions.Value;
        _tokenInfoOptions = tokenInfoOptions.Value;
        _assetsInfoOptions = assetsInfoOptions.Value;
        _logger = logger;
        _chainOptions = chainOptions.Value;
    }

    public string GetTokenId(string chainId, string symbol)
    {
        return $"{chainId}-{symbol}";
    }
    
    public string BuildSymbolImageUrl(string symbol)
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
    
    public async Task<TokenInfoDto> GetAsync(string chainId, string symbol)
    {
        var tokenKey = $"{CommonConstant.TokenInfoCachePrefix}:{symbol}:{chainId}";
        var tokenInfo = await _tokenInfoCache.GetAsync(tokenKey);
        if (tokenInfo != null)
        {
            return tokenInfo;
        }
        
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTokenInfoApi;
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
            ImageUri = BuildSymbolImageUrl(tokenInfoResult.Symbol),
            TokenName = tokenInfoResult.TokenName,
            Address = _chainOptions.ChainInfos[chainId].TokenContractAddress
        };

        _tokenInfoCache.SetAsync(tokenKey, tokenInfo, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
        });
        return tokenInfo;
    }
}