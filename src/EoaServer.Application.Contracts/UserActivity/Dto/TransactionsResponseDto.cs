using System;
using System.Collections.Generic;
using EoaServer.Commons;

namespace EoaServer.UserActivity.Dto;

public class TransactionsResponseDto
{
    public long Total { get; set; }
    public List<TransactionResponseDto> Transactions { get; set; } = new List<TransactionResponseDto>();
}

public class TransactionResponseDto
{
    public string TransactionId { get; set; }

    public long BlockHeight { get; set; }

    public string Method { get; set; }

    public TransactionStatus Status { get; set; }
    public CommonAddressDto From { get; set; }

    public CommonAddressDto To { get; set; }

    public long Timestamp { get; set; }

    public string TransactionValue { get; set; }

    public string TransactionFee { get; set; }

    public DateTime BlockTime { get; set; }

    public List<string> ChainIds { get; set; } = new();
}