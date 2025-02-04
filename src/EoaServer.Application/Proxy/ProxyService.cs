using System;
using System.Net.Http;
using System.Threading.Tasks;
using EoaServer.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace EoaServer.Proxy;

[RemoteService(false)]
[DisableAuditing]
public class ProxyService : EoaServerBaseService, IProxyService
{
    private readonly DidServerOptions _didServerOptions;
    private readonly HttpClient _httpClient;

    public ProxyService(IOptionsSnapshot<DidServerOptions> didServerOptions, IHttpClientFactory httpClientFactory)
    {
        _didServerOptions = didServerOptions.Value;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<object> ProxyGetRequestAsync(string targetUrl)
    {
        var targetRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(_didServerOptions.BaseUrl + "/" + targetUrl),
        };
        
        var response = await _httpClient.SendAsync(targetRequest, HttpCompletionOption.ResponseHeadersRead);
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<object?>(responseBody);
        return new JsonResult(result)
        {
            ContentType = "application/json"
        };
    }
}