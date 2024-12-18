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
    private readonly TokenInfoOptions _tokenInfoOptions;
    private readonly AssetsInfoOptions _assetsInfoOptions;
    private readonly ILogger<TokenInfoProvider> _logger;
    private readonly ChainOptions _chainOptions;
    private readonly IAElfScanDataProvider _aelfScanDataProvider;
    
    public TokenInfoProvider(IDistributedCache<TokenInfoDto> tokenCache,
        IOptionsSnapshot<TokenInfoOptions> tokenInfoOptions,
        IOptionsSnapshot<AssetsInfoOptions> assetsInfoOptions,
        ILogger<TokenInfoProvider> logger,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IAElfScanDataProvider aelfScanDataProvider)
    {
        _tokenInfoCache = tokenCache;
        _tokenInfoOptions = tokenInfoOptions.Value;
        _assetsInfoOptions = assetsInfoOptions.Value;
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _aelfScanDataProvider = aelfScanDataProvider;
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
        
        var tokenInfoResult = await _aelfScanDataProvider.GetIndexerTokenInfoAsync(chainId, symbol);

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