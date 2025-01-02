namespace EoaServer.Provider.Dto.Indexer;

public enum SortField
{
    Id,
    BlockTime,
    BlockHeight,
    HolderCount,
    TransferCount,
    Symbol,
    FormatAmount,
    Address,
    TransactionId
}

public enum SortDirection
{
    Asc,
    Desc
}