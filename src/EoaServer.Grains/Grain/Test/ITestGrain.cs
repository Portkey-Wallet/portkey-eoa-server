namespace EoaServer.Grain.Test;

public interface ITestGrain: IGrainWithGuidKey
{
    Task Create(TestGrainDto grainDto);
    Task<GrainResultDto<TestGrainDto>> Get();
}