using System.Collections.Generic;
using System.Threading.Tasks;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.UserAssets.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace EoaServer.Common.Provider;

public class DidServerProvider : IDidServerProvider, ISingletonDependency
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly DidServerOptions _didServerOptions;
    private readonly ILogger<AElfScanDataProvider> _logger;

    public DidServerProvider(IHttpClientProvider httpClientProvider, IOptionsSnapshot<DidServerOptions> didServerOptions, ILogger<AElfScanDataProvider> logger)
    {
        _httpClientProvider = httpClientProvider;
        _didServerOptions = didServerOptions.Value;
        _logger = logger;
    }

    public async Task<NftItem> SetTraitsPercentAsync(string traits)
    {
        var url = _didServerOptions.BaseUrl + "/" + CommonConstant.DidSetTraitsPercentage;
        var requestUrl = $"{url}?traits={traits}";
        var nftItem = await _httpClientProvider.GetAsync<NftItem>(requestUrl, new Dictionary<string, string>());
        if (nftItem == null)
        {
            _logger.LogError($"Http request: {requestUrl} get null response");
        }
        return nftItem;
    }
}