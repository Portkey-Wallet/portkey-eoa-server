using System;
using System.Collections.Generic;
using EoaServer.Token.Dto;
using MongoDB.Driver;

namespace EoaServer.Provider.Dto.Indexer;

public class TokenTransferInput : BaseInput

{
    public string Symbol { get; set; } = "";
    public string Search { get; set; } = "";
    public string CollectionSymbol { get; set; } = "";

    public string Address { get; set; } = "";

    public List<SymbolType> Types { get; set; } = new() { SymbolType.Token };

    public string FuzzySearch { get; set; } = "";

    public DateTime? BeginBlockTime { get; set; }

    public void SetDefaultSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockTime, SortDirection.Desc), (SortField.TransactionId, SortDirection.Desc));
    }


    public void SetBlockTimeSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockTime, SortDirection.Desc), (SortField.TransactionId, SortDirection.Desc));
    }
}