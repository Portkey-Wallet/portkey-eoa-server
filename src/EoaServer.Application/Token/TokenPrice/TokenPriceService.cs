using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token.Dto;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace EoaServer.Token.TokenPrice;

public class TokenPriceService : ITokenPriceService, ISingletonDependency
{
    private readonly ILogger<TokenPriceService> _logger;
    private readonly IEnumerable<ITokenPriceProvider> _tokenPriceProviders;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IOptionsMonitor<TokenPriceWorkerOption> _tokenPriceWorkerOption;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

    public TokenPriceService(ILogger<TokenPriceService> logger, IEnumerable<ITokenPriceProvider> tokenPriceProviders,
        IDistributedCache<string> distributedCache, IOptionsMonitor<TokenPriceWorkerOption> tokenPriceWorkerOption)
    {
        _logger = logger;

        // if (tokenPriceProviders != null)
        // {
        //     _tokenPriceProviders = tokenPriceProviders.OrderBy(provider => provider.GetPriority());
        // }

        _tokenPriceProviders = tokenPriceProviders;
        _distributedCache = distributedCache;
        _tokenPriceWorkerOption = tokenPriceWorkerOption;
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public async Task<TokenPriceDataDto> GetCurrentPriceAsync(string symbol)
    {
        try
        {
            var key = GetSymbolPriceKey(symbol);
            var priceString = await _distributedCache.GetAsync(key);
            if (priceString.IsNullOrEmpty())
            {
                return new TokenPriceDataDto
                {
                    Symbol = symbol,
                    PriceInUsd = 0
                };
            }

            decimal price;
            if (!decimal.TryParse(priceString, out price))
            {
                _logger.LogError("An error occurred while retrieving the token price, {0}-{1}", symbol, priceString);
                throw new UserFriendlyException("An error occurred while retrieving the token price.");
            }

            return new TokenPriceDataDto
            {
                Symbol = symbol,
                PriceInUsd = price
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while retrieving the token price, {0}", symbol);
            throw;
        }
    }

    public async Task RefreshCurrentPriceAsync(string symbol = default)
    {
        if (_tokenPriceProviders == null)
        {
            _logger.LogError("token price providers is null.");
            return;
        }
        
        foreach (var tokenPriceProvider in _tokenPriceProviders)
        {
            var symbols = symbol != null ? new[] { symbol } : _tokenPriceWorkerOption.CurrentValue.Symbols.ToArray();

            try
            {
                // if (tokenPriceProvider.GetType().Name == nameof(FeiXiaoHaoTokenPriceProvider))
                // {
                //     symbols = symbols.Where(t => t != CommonConstant.SgrSymbolName).ToArray();
                // }
                // else
                // {
                //     symbols = symbols.Where(t => t == CommonConstant.SgrSymbolName).ToArray();
                // }

                var prices = await tokenPriceProvider.GetPriceAsync(symbols);
                if (prices.IsNullOrEmpty())
                {
                    continue;
                }

                foreach (var price in prices)
                {
                    var key = GetSymbolPriceKey(price.Key);
                    var value = price.Value.ToString(CultureInfo.InvariantCulture);
                    await _distributedCache.SetAsync(key, value, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
                    });
                    _logger.LogInformation("refresh current price success:{0}-{1}", key, value);
                }

                _logger.LogInformation("refresh current price success, the provider used is: {0}",
                    tokenPriceProvider.GetType().ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "get token price error. {0}", tokenPriceProvider.GetType().ToString());
            }
        }
    }
    

    private string GetSymbolPriceKey(string symbol)
    {
        return
            $"{_tokenPriceWorkerOption.CurrentValue.Prefix}:{_tokenPriceWorkerOption.CurrentValue.PricePrefix}:{symbol?.ToUpper()}";
    }

    private string GetSymbolPriceKey(string symbol, string dateTime)
    {
        return
            $"{_tokenPriceWorkerOption.CurrentValue.Prefix}:{_tokenPriceWorkerOption.CurrentValue.PricePrefix}:{symbol?.ToUpper()}:{dateTime}";
    }
}