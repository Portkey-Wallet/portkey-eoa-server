using System;
using System.Collections.Generic;

namespace EoaServer.Provider.Dto.Indexer;

public class TransactionsRequestDto : BaseInput
{
    public string TransactionId { get; set; } = "";
    public int BlockHeight { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public string Address { get; set; } = "";

    public void SetDefaultSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockTime, SortDirection.Desc), (SortField.TransactionId, SortDirection.Desc));
    }


    public void SetFirstTransactionSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockHeight, SortDirection.Asc), (SortField.TransactionId, SortDirection.Asc));
    }

    public void SetLastTransactionSort()
    {
        if (!OrderBy.IsNullOrEmpty() || !OrderInfos.IsNullOrEmpty())
        {
            return;
        }

        OfOrderInfos((SortField.BlockHeight, SortDirection.Desc), (SortField.TransactionId, SortDirection.Desc));
    }
}
