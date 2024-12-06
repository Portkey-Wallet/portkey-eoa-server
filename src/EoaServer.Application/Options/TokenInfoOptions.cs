using System.Collections.Generic;

namespace EoaServer.Options;

public class TokenInfoOptions
{
    public Dictionary<string, TokenInfo> TokenInfos { get; set; }
}

public class TokenInfo
{
    public string ImageUrl { get; set; }
}