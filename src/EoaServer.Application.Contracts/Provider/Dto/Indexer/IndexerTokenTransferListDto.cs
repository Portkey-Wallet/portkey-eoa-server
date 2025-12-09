using System.Collections.Generic;
using EoaServer.Token.Dto;

namespace EoaServer.Provider.Dto.Indexer;

public class IndexerTokenTransfersDto
{
    public IndexerTokenTransferListDto TransferInfo { get; set; }
}

public class IndexerTokenTransferListDto
{
    public long TotalCount  { get; set; }
    public List<IndexerTransferInfoDto> Items { get; set; } = new();
}

public class IndexerTransferInfoDto
{
    public string TransactionId { get; set; }
    public MetadataDto Metadata { get; set; }
}