using EoaServer.State.Test;
using Volo.Abp.ObjectMapping;

namespace EoaServer.Grain.Test;

public class TestGrain : Grain<TestState>, ITestGrain
{
    private readonly IObjectMapper _objectMapper;

    public TestGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    
    public async Task Create(TestGrainDto grainDto)
    {
        State = _objectMapper.Map<TestGrainDto, TestState>(grainDto);
        await WriteStateAsync();
    }

    public async Task<GrainResultDto<TestGrainDto>> Get()
    {
        return new GrainResultDto<TestGrainDto>()
        {
            Data = _objectMapper.Map<TestState, TestGrainDto>(State)
        };
    }
    
}