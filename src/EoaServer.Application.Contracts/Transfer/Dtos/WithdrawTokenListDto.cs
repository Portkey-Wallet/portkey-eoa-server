using System.Collections.Generic;
using EoaServer.Commons;

namespace EoaServer.Transfer.Dtos;

public class WithdrawTokenListDto : ChainDisplayNameDto
{
    public List<TokenConfigDto> TokenList { get; set; }
}

public class TokenConfigDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string ContractAddress { get; set; }
}