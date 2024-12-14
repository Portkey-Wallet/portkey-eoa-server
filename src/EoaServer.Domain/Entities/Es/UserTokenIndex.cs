using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace EoaServer.Entities.Es;


public class UserTokenIndex : EoaBaseEsEntity<Guid>, IIndexBuild
{
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public bool IsDisplay { get; set; }
    [Keyword] public bool IsDefault { get; set; }
    [Keyword] public int SortWeight { get; set; }
    public Token Token { get; set; }
}

public class Token
{
    [Keyword] public string Id { get; set; }
    [Keyword] public string ChainId {get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string Symbol { get; set; }
    public int Decimals { get; set; }
}