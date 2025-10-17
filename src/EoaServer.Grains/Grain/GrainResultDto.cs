using EoaServer.Commons;

namespace EoaServer.Grain;

[GenerateSerializer]
public class GrainResultDto<T> : GrainResultDto
{
    [Id(0)] public T Data { get; set; }
}

[GenerateSerializer]
public class GrainResultDto
{
    [Id(0)] public string Code { get; set; } = CommonConstant.SuccessCode;
    [Id(1)] public string Message { get; set; }
    
    public bool Success() => Code == CommonConstant.SuccessCode;
}