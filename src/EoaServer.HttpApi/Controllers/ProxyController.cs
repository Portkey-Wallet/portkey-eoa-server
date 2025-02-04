using System.Threading.Tasks;
using Asp.Versioning;
using EoaServer.Proxy;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace EoaServer.Controllers;

[RemoteService]
[Area("proxy")]
[ControllerName("Proxy")]
[Route("/api/app/proxy")]
public class ProxyController : EoaServerBaseController
{
    private readonly IProxyService _proxyService;

    public ProxyController(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    [HttpGet]
    [Route("{**proxyUrl}")]
    public async Task<object> ProxyAsync(string proxyUrl)
    {
        return await _proxyService.ProxyGetRequestAsync(proxyUrl);
    }
}