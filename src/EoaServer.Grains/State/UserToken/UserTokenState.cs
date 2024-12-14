namespace EoaServer.State.UserToken;

public class UserTokenState
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsDisplay { get; set; }
    public bool IsDefault { get; set; }
    public int SortWeight { get; set; }
    public Token Token { get; set; }
    public bool IsDelete { get; set; }
}

public class Token
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
}