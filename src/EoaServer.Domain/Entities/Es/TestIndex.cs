using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace EoaServer.Entities.Es;

public class TestIndex: EoaBaseEsEntity<Guid>, IIndexBuild
{
    [Keyword] public override Guid Id { get; set; }
    [Keyword] public string Content { get; set; }
    public int Count { get; set; }
}