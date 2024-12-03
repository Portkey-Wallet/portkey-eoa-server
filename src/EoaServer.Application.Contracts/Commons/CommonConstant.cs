using System;
using System.Collections.Generic;

namespace EoaServer.Commons;

public static class CommonConstant
{
    public const string JwtPrefix = "Bearer";
    public const string AuthHeader = "Authorization";
    public static DateTimeOffset DefaultAbsoluteExpiration = DateTime.Parse("2099-01-01 12:00:00");

    public const string SuccessCode = "20000";

    public const string MainChainId = "AELF";
    public const string TDVVChainId = "tDVV";
    public const string TDVWChainId = "tDVW";
    public static List<string> ChainIds = new List<string> { MainChainId, TDVWChainId, TDVVChainId };
}