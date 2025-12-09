using System;
using System.Threading.Tasks;
using EoaServer.Grain.Test;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace EoaServer.Test;

[RemoteService(false), DisableAuditing]
public class TestAppService : EoaServerBaseService, ITestAppService
{
    private readonly ILogger<TestAppService> _logger;
    private readonly IClusterClient _clusterClient;

    public TestAppService(ILogger<TestAppService> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task<string> TestAsync()
    {
        var grainId = Guid.NewGuid();
        var grain = _clusterClient.GetGrain<ITestGrain>(grainId);
        await grain.Create(new TestGrainDto()
        {
            Id = grainId,
            Content = "test",
            Count = 1
        });

        var testDto = await grain.Get();
        _logger.LogInformation(JsonConvert.SerializeObject(testDto.Data));
        return "success";
    }
}