using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token.Dto;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace EoaServer.Token;


public interface ITokenInfoProvider
{
    Task<TokenInfoDto> GetAsync(string chainId, string symbol);
    string BuildSymbolImageUrl(string symbol);
    string GetTokenId(string chainId, string symbol);
    Task<Dictionary<string, TokenInfoDto>> GetTokenMapAsync(HashSet<string> tokens);
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
            return new TokenInfoDto();
        }
        
        tokenInfo = new TokenInfoDto
        {
            Symbol = tokenInfoResult.Symbol,
            Decimals = tokenInfoResult.Decimals,
            ChainId = tokenInfoResult.IssueChainId,
            ImageUrl = BuildSymbolImageUrl(tokenInfoResult.Symbol),
            TokenName = tokenInfoResult.TokenName,
            TokenContractAddress = _chainOptions.ChainInfos[chainId].TokenContractAddress,
            Id = chainId + "_" + symbol,
            TotalSupply = tokenInfoResult.TotalSupply,
            Issuer = tokenInfoResult.Issuer,
            IsBurnable = tokenInfoResult.IsBurnable,
        };
        
        ChainDisplayNameHelper.SetDisplayName(tokenInfo, chainId);

        await _tokenInfoCache.SetAsync(tokenKey, tokenInfo, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
        });
        return tokenInfo;
    }
    
    public async Task<Dictionary<string, TokenInfoDto>> GetTokenMapAsync(HashSet<string> tokens)
    {
        var result = tokens.ToDictionary(t => t, t => new TokenInfoDto());

        var sideChain = _chainOptions.ChainInfos.FirstOrDefault(t => t.Value.IsMainChain == false);
        var tokenChain = sideChain.Value.ChainId;
        
        var mapTasks = result.Select(async token =>
        {
            return await GetAsync(tokenChain, token.Key);
        }).ToList();

        var tokenList = await Task.WhenAll(mapTasks);
        foreach (var tokenInfo in tokenList)
        {
            if (tokenInfo != null && !tokenInfo.Symbol.IsNullOrEmpty())
            {
                result[tokenInfo.Symbol] = tokenInfo;
            }
        }
        return result;
    }
}