using System.Collections.Generic;

namespace EoaServer.Options;

public class GraphQLOptions
{
    public Dictionary<string, IndexerOption> IndexerOptions { get; set; }
}

public class IndexerOption
{
    public string BaseUrl { get; set; }
}