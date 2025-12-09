using System.Collections.Generic;
using EoaServer.Token.Dto;

namespace EoaServer.UserAssets.Dtos;

public class GetAddressTokenListResultDto
{
    public decimal AssetInUsd { get; set; }
    public decimal AssetInElf { get; set; }
    public long Total { get; set; }
    public List<TokenInfoDto> List { get; set; } = new();
}

public class TokenInfoDto
{
    public TokenBaseInfo Token { set; get; }
    public decimal Quantity { set; get; }
    public decimal ValueOfUsd { set; get; }
    public decimal PriceOfUsd { set; get; }
    public double PriceOfUsdPercentChange24h { get; set; }
    public decimal PriceOfElf { set; get; }
    public decimal ValueOfElf { set; get; }
    public List<string> ChainIds { set; get; }
    
    public SymbolType Type { get; set; }
}

public class TokenBaseInfo
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string ImageUrl { get; set; }
    public int Decimals { get; set; }
}