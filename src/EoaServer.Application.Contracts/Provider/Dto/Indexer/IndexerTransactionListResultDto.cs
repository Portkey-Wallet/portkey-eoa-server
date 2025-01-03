using System.Collections.Generic;
using EoaServer.Commons;
using EoaServer.Token.Dto;

namespace EoaServer.Provider.Dto.Indexer;

public class IndexerTransactionResultDto
{
    public IndexerTransactionListResultDto TransactionInfos { get; set; }
}

public class IndexerTransactionListResultDto
{
    public long TotalCount { get; set; }
    public List<IndexerTransactionInfoDto> Items { get; set; } = new();
}

public class IndexerTransactionInfoDto
{
    public string TransactionId { get; set; }
    public long BlockHeight { get; set; }
    public string MethodName { get; set; }
    public TransactionStatus Status { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public long TransactionValue { get; set; }
    public MetadataDto Metadata { get; set; }
    public long Fee { get; set; }
}