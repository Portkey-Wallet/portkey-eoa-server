using System;
using System.Runtime.Serialization;

namespace EoaServer.Token.TokenPrice.CoinGecko;

public class RequestExceedingLimitException: Exception
{
    public RequestExceedingLimitException()
    {
    }

    public RequestExceedingLimitException(string message) : base(message)
    {
    }

    public RequestExceedingLimitException(string message, Exception inner) : base(message, inner)
    {
    }

    protected RequestExceedingLimitException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}