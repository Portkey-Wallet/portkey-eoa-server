namespace EoaServer.Token.Dto;

public class TokenInfoDto
{
    public string Symbol { get; set; }   
    public string Address { get; set; }
    public int Decimals { get; set; }
    public string ImageUri { get; set; }
    public string ChainId { get; set; }
    public string TokenName { get; set; }
}