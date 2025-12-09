using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace EoaServer.Proxy;

public interface IProxyService
{
    Task<object> ProxyGetRequestAsync(string targetUrl);
}