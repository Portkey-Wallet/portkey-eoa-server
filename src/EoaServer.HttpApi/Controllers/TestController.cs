using System.Threading.Tasks;
using Asp.Versioning;
using EoaServer.Test;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace EoaServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Test")]
[Route("api/test")]
public class TestController : EoaServerBaseController
{
    private readonly ITestAppService _testAppService;

    public TestController(ITestAppService testAppService)
    {
        _testAppService = testAppService;
    }

    [HttpPost]
    public async Task<string> TestAsync()
    {
        return await _testAppService.TestAsync();
    }
}