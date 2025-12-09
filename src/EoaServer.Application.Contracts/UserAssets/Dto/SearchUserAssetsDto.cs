using System.Collections.Generic;
using EoaServer.Commons;

namespace EoaServer.UserAssets.Dto;

public class SearchUserAssetsV2Dto
{
    public List<TokenInfoV2Dto> TokenInfos { get; set; } = new();
    public List<NftCollectionDto> NftInfos { get; set; } = new();
    public long TotalRecordCount { get; set; }
}

public class NftInfoDto : ChainDisplayNameDto
{
    public string ImageUrl { get; set; }
    public string Alias { get; set; }
    public string TokenId { get; set; }
    public string CollectionName { get; set; } // nftItem collectionSymbol
    public string Balance { get; set; }
    public string TokenContractAddress { get; set; }
    public string Decimals { get; set; }
    public string TokenName { get; set; }
    public bool IsSeed { get; set; }
    public int SeedType { get; set; }
    public string Label { get; set; } // new nftItem
    public string Symbol { get; set; }
}

public class TokenInfoV2Dto : ChainDisplayNameDto
{
    public string Symbol { get; set; }
    public string Address { get; set; } // user address
    public string Label { get; set; }
    
    public string Balance { get; set; }
    public string Decimals { get; set; }
    public string BalanceInUsd { get; set; }
    public string TokenContractAddress { get; set; }
    public string ImageUrl { get; set; }
}

public class NftCollectionDto
{
    public string CollectionName { get; set; }
    public string ImageUrl { get; set; }
    public List<NftInfoDto> Items { get; set; } = new();
}