namespace EoaServer.Grain.Test;

[GenerateSerializer]
public class TestGrainDto
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Content { get; set; }
    [Id(2)] public int Count { get; set; }
}