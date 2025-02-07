using System.Collections.Generic;

namespace EoaServer.Awaken;

public class TradePairsDto
{
    public long TotalCount { get; set; }
    public List<TradePairsItemDto> Items { get; set; }
}