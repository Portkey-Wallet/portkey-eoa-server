using System.Collections.Generic;

namespace EoaServer.Options;

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class ChainInfo
{
    public string ChainId { get; set; }
    public string BaseUrl { get; set; }
    public string ContractAddress { get; set; }
    public string PublicKey { get; set; }
    public bool IsMainChain { get; set; }
}