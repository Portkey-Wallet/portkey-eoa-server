namespace EoaServer.Grain.User;

[GenerateSerializer]
public class UserGrainDto
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public string Address { get; set; }
    [Id(3)] public string WalletName { get; set; }
    [Id(4)] public DateTime CreateTime { get; set; }
}