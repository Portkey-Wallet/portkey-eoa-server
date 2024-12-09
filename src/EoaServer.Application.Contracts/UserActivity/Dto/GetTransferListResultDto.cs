using System;
using System.Collections.Generic;
using EoaServer.Commons;

namespace EoaServer.UserActivity.Dto;

public class GetTransferListResultDto
{
    public long Total { get; set; }

    public List<TokenTransferInfoDto> List { get; set; }
}

public class TokenTransferInfoDto
{
    public string ChainId { get; set; }
    public List<string> ChainIds { get; set; } = new();
    public string TransactionId { get; set; }
    public string Method { get; set; }
    public long BlockHeight { get; set; }
    public long BlockTime { get; set; }
    public string Symbol { get; set; }

    public string SymbolName { get; set; }

    public DateTime DateTime
    {
        get => DateTimeOffset.FromUnixTimeSeconds(BlockTime).DateTime;
    }

    public string SymbolImageUrl { get; set; }
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }
    public decimal Quantity { get; set; }
    

    public TransactionStatus Status { get; set; }

    public List<TransactionFeeDto> TransactionFeeList { get; set; }
}

public class TransactionFeeDto 
{
    public string Symbol { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountOfUsd { get; set; }
}