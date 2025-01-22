using System;

namespace EoaServer.Search.Dto;

public class ChainsInfoDto
{
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string EndPoint { get; set; }
    public string ExplorerUrl { get; set; }
    public string CaContractAddress { get; set; }
    public string DisplayChainName { get; set; }
    public string ChainImageUrl { get; set; }
    public DefaultTokenInfoDto DefaultToken { get; set; }
    public DateTime LastModifyTime { get; set; }
}

public class DefaultTokenInfoDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string ImageUrl { get; set; }
    public string Symbol { get; set; }
    public string Decimals { get; set; }
    public long IssueChainId { get; set; }
}