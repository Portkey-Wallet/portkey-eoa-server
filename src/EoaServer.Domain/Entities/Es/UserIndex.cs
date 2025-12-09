using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace EoaServer.Entities.Es;

public class UserIndex : EoaBaseEsEntity<Guid>, IIndexBuild
{
    [Keyword] public override Guid Id { get; set; }
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string WalletName { get; set; }
    public DateTime CreateTime { get; set; }
}