using System.Collections.Generic;
using EoaServer.Commons;

namespace EoaServer.UserAssets.Dtos;

public class GetTokenDto
{
    public List<Token> Data { get; set; }
    public string TotalBalanceInUsd { get; set; }
    public long TotalRecordCount { get; set; }
}

public class Token : ChainDisplayNameDto
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public string Balance { get; set; }
    public int Decimals { get; set; }
    public string BalanceInUsd { get; set; }
    public string TokenContractAddress { get; set; }
    public string ImageUrl { get; set; }
    public string Label { get; set; }

    public string DisplayChainName { get; set; }

    public string ChainImageUrl { get; set; }
}