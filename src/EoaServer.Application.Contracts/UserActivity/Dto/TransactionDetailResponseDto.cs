using System.Collections.Generic;
using Google.Protobuf;


namespace EoaServer.UserActivity.Dto;

public class TransactionDetailResponseDto
{
    public List<TransactionDetailDto> List { get; set; } = new List<TransactionDetailDto>();
}

public class TransactionDetailDto
{
    public string TransactionId { get; set; }
    public Commons.TransactionStatus Status { get; set; }
    public long BlockHeight { get; set; }
    public long BlockConfirmations { get; set; }
    public long Timestamp { get; set; }
    public string Method { get; set; }


    public CommonAddressDto From { get; set; }

    public CommonAddressDto To { get; set; }
    public List<TokenTransferredDto> TokenTransferreds { get; set; } = new List<TokenTransferredDto>();
    public List<NftsTransferredDto> NftsTransferreds { get; set; } = new List<NftsTransferredDto>();

    public List<ValueInfoDto> TransactionValues { get; set; }

    public List<ValueInfoDto> TransactionFees { get; set; }

    public string ResourcesFee { get; set; }

    public List<ValueInfoDto> BurntFees { get; set; } = new List<ValueInfoDto>();

    public string TransactionRefBlockNumber { get; set; }

    public string TransactionRefBlockPrefix { get; set; }

    public string TransactionParams { get; set; }

    public string ReturnValue { get; set; }

    public string TransactionSignature { get; set; }

    public bool Confirmed { get; set; }

    public string Version { get; set; }

    public string Bloom { get; set; }
    public string Error { get; set; }

    public string TransactionSize { get; set; }

    public string ResourceFee { get; set; }

    public List<LogEventInfoDto> LogEvents { get; set; } = new List<LogEventInfoDto>();

    public List<IMessage> ParseLogEvents { get; set; } = new();

    public void AddParseLogEvents(IMessage message)
    {
        if (message != null)
        {
            ParseLogEvents.Add(message);
        }
    }
}

public class LogEventInfoDto
{
    public CommonAddressDto ContractInfo { get; set; }


    public string EventName { get; set; }

    public string Indexed { get; set; }

    public string NonIndexed { get; set; }
}

public class ValueInfoDto
{
    public string Symbol { get; set; }
    public long Amount { get; set; }

    public string AmountString { get; set; }
    public string NowPrice { get; set; }
    public string TradePrice { get; set; }
}

public class TokenTransferredDto
{
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }
    public string Symbol { get; set; }
    public string Name { get; set; }

    public long Amount { get; set; }

    public string AmountString { get; set; }

    public string TradePrice { get; set; }
    public string NowPrice { get; set; }
    public string ImageUrl { get; set; }
    public string ImageBase64 { get; set; }
}

public class NftsTransferredDto
{
    public CommonAddressDto From { get; set; }
    public CommonAddressDto To { get; set; }
    public string Symbol { get; set; }

    public string Name { get; set; }
    public long Amount { get; set; }
    public string AmountString { get; set; }
    public string TradePrice { get; set; }
    public string NowPrice { get; set; }
    public string ImageUrl { get; set; }
    public string ImageBase64 { get; set; }
    public bool IsCollection { get; set; }
}

public class CommonAddressDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public AddressType AddressType { get; set; }
    public bool IsManager { get; set; }
    public bool IsProducer { get; set; }
}

public enum AddressType
{
    EoaAddress,
    ContractAddress
}