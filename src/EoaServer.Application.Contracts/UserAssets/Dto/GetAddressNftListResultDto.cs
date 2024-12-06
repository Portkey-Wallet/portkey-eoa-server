using System;
using System.Collections.Generic;

namespace EoaServer.UserAssets.Dtos;

public class GetAddressNftListResultDto
{
    public long Total { get; set; }
    public List<AddressNftInfoDto> List { get; set; }
}

public class AddressNftInfoDto
{
    public TokenBaseInfo NftCollection { get; set; }
    public TokenBaseInfo Token { get; set; }
    public decimal Quantity { set; get; }
    public long TransferCount { get; set; }
    public DateTime? FirstNftTime { get; set; }

    public List<string> ChainIds { get; set; } = new();
}