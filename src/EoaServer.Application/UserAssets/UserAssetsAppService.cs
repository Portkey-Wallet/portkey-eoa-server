using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token;
using EoaServer.Token.Dto;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using EoaServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using TokenInfoDto = EoaServer.UserAssets.Dtos.TokenInfoDto;

namespace EoaServer.UserAssets;


[RemoteService(false)]
[DisableAuditing]
public class UserAssetsAppService : EoaServerBaseService, IUserAssetsAppService
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly AElfScanOptions _aelfScanOptions;
    private readonly TokenListOptions _tokenListOptions;
    private readonly SeedImageOptions _seedImageOptions;
    private readonly IImageProcessProvider _imageProcessProvider;
    private readonly IpfsOptions _ipfsOptions;
    private readonly ITokenInfoAppService _tokenInfoAppService;
    private readonly NftItemDisplayOption _nftItemDisplayOption;
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<UserAssetsAppService> _logger;

    public UserAssetsAppService(IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<AElfScanOptions> aElfScanOptions,
        IOptionsSnapshot<TokenListOptions> tokenListOptions,
        IOptionsSnapshot<SeedImageOptions> seedImageOptions,
        IImageProcessProvider imageProcessProvider,
        IOptionsSnapshot<IpfsOptions> ipfsOption,
        IOptionsSnapshot<ChainOptions> chainOptions,
        ITokenInfoAppService tokenInfoAppService,
        ILogger<UserAssetsAppService> logger)
    {
        _httpClientProvider = httpClientProvider;
        _aelfScanOptions = aElfScanOptions.Value;
        _tokenListOptions = tokenListOptions.Value;
        _seedImageOptions = seedImageOptions.Value;
        _imageProcessProvider = imageProcessProvider;
        _ipfsOptions = ipfsOption.Value;
        _tokenInfoAppService = tokenInfoAppService;
        _chainOptions = chainOptions.Value;
        _logger = logger;
    }
    
    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        var tokenList = new GetAddressTokenListResultDto();
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanUserTokenAssetsApi;

        foreach (var addressInfo in requestDto.AddressInfos)
        {
            var requestUrl = $"{url}?address={addressInfo.Address}&chainId={addressInfo.ChainId}&skipCount={requestDto.SkipCount}&MaxResultCount={requestDto.MaxResultCount}";
            var chainTokenList = await _httpClientProvider.GetDataAsync<GetAddressTokenListResultDto>(requestUrl);
            tokenList.AssetInUsd += chainTokenList.AssetInUsd;
            tokenList.AssetInElf += chainTokenList.AssetInElf;
            tokenList.Total += chainTokenList.Total;
            tokenList.List.AddRange(chainTokenList.List);
        }
        
        AddDefaultTokens(tokenList);
        
        var result = await ConvertDtoAsync(tokenList, requestDto);
        
        result.Data = SortTokens(result.Data);
        
        return result;
    }
    
    private List<TokenWithoutChain> SortTokens(List<TokenWithoutChain> tokens)
    {
        var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();
        
        return tokens.OrderBy(t => decimal.Parse(t.Balance) == 0)
            .ThenBy(t => t.Symbol != CommonConstant.ELF)
            .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
            .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
            .ThenBy(t => t.Symbol)
            .ToList();
    }
    
    public async Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto)
    {
        var nftList = new GetAddressNftListResultDto();
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanUserNFTAssetsApi;

        foreach (var addressInfo in requestDto.AddressInfos)
        {
            // get all NFT for TotalNftItemCount
            var requestUrl = $"{url}?Address={addressInfo.Address}&ChainId={addressInfo.ChainId}&SkipCount=0&MaxResultCount={LimitedResultRequestDto.MaxMaxResultCount}";
            var chainTokenList = await _httpClientProvider.GetDataAsync<GetAddressNftListResultDto>(requestUrl);
            nftList.Total += chainTokenList.Total;
            nftList.List.AddRange(chainTokenList.List);
        }
        
        var result = new GetNftCollectionsDto()
        {
            TotalNftItemCount = nftList.Total
        };

        result.Data = new List<NftCollection>();
        foreach (var nftInfoDto in nftList.List)
        {
            var collection = result.Data.FirstOrDefault(t => t.Symbol == nftInfoDto.NftCollection.Symbol && t.ChainId == nftInfoDto.ChainIds[0]);
            if (collection == null)
            {
                var resultNftInfo = new NftCollection
                {
                    ChainId = nftInfoDto.ChainIds[0],
                    CollectionName = nftInfoDto.NftCollection.Name,
                    ItemCount = 1,
                    Symbol = nftInfoDto.NftCollection.Symbol,
                    IsSeed = nftInfoDto.NftCollection.Symbol.StartsWith(TokensConstants.SeedNamePrefix)
                };
            
                var image = _seedImageOptions.SeedImageDic.TryGetValue(nftInfoDto.NftCollection.Symbol, out var imageUrl) ? 
                    imageUrl : nftInfoDto.NftCollection.ImageUrl;
                resultNftInfo.ImageUrl = await _imageProcessProvider.GetResizeImageAsync(
                    image, requestDto.Width, requestDto.Height,
                    ImageResizeType.Forest);
            
                result.Data.Add(resultNftInfo);
            }
            else
            {
                collection.ItemCount += 1;
            }
        }

        result.TotalRecordCount = nftList.List.Count();
        
        nftList.List = nftList.List.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();
        
        TryUpdateImageUrlForCollections(result.Data);
        DealWithDisplayChainImage(result);
        
        return result;
    }

    public async Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        var nftItems = await GetUserCollectionItemsAsync(requestDto.AddressInfos, requestDto.Symbol);
        var result = new GetNftItemsDto()
        {
            TotalRecordCount = nftItems.Count,
            Data = new List<NftItem>()
        };

        nftItems = nftItems.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();
        
        var mapTasks = nftItems.Select(async nftItem =>
        {
            return await _tokenInfoAppService.GetIndexerTokenInfoAsync(nftItem.ChainIds[0], nftItem.Token.Symbol);
        }).ToList();

        var tokenList = await Task.WhenAll(mapTasks);
        
        foreach (var nftItem in nftItems)
        {
            var tokenInfo = tokenList.FirstOrDefault(t => t.Symbol == nftItem.Token.Symbol);
            
            var tokenContractAddress = nftItem.ChainIds.Count > 0 && _chainOptions.ChainInfos.ContainsKey(nftItem.ChainIds[0])
                ? _chainOptions.ChainInfos[nftItem.ChainIds[0]].TokenContractAddress
                : null;
            var resultNftItem = new NftItem
            {
                ChainId = nftItem.ChainIds[0],
                Symbol = nftItem.Token.Symbol,
                TokenId = TokenHelper.GetNFTItemId(nftItem.Token.Symbol).ToString(),
                Alias = nftItem.Token.Name,
                Balance = nftItem.Quantity.ToString(),
                TotalSupply = tokenInfo?.TotalSupply ?? 0,
                CirculatingSupply = tokenInfo?.Supply ?? 0,
                TokenContractAddress = tokenContractAddress,
                Decimals = nftItem.Token.Decimals.ToString(),
                CollectionSymbol = nftItem.NftCollection.Symbol,
                TokenName = nftItem.Token.Name,
                Description = null // todo
            };

            SetNftInfo(resultNftItem, tokenInfo);
            
            resultNftItem.ImageUrl =
                await _imageProcessProvider.GetResizeImageAsync(nftItem.Token.ImageUrl, requestDto.Width,
                    requestDto.Height,
                    ImageResizeType.Forest);
            resultNftItem.ImageLargeUrl = await _imageProcessProvider.GetResizeImageAsync(nftItem.Token.ImageUrl,
                (int)ImageResizeWidthType.IMAGE_WIDTH_TYPE_ONE, (int)ImageResizeHeightType.IMAGE_HEIGHT_TYPE_AUTO,
                ImageResizeType.Forest);
            
            resultNftItem.RecommendedRefreshSeconds = _nftItemDisplayOption.RecommendedRefreshSeconds <= 0
                ? NftItemDisplayOption.DefaultRecommendedRefreshSeconds
                : _nftItemDisplayOption.RecommendedRefreshSeconds;
            
            result.Data.Add(resultNftItem);
        }
        
        SetSeedStatusAndTypeForNftItems(result.Data);

        OptimizeSeedAliasDisplayForNftItems(result.Data);

        TryUpdateLimitPerMintForInscription(result.Data);

        TryUpdateImageUrlForNftItems(result.Data);

        await TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(result.Data);

        CalculateAndSetTraitsPercentageAsync(result.Data);
        
        return result;
    }

    private void SetNftInfo(NftItem item, IndexerTokenInfoDto indexerTokenInfosDto)
    {
        if (indexerTokenInfosDto == null)
        {
            return;
        }
        
        var externalInfo = indexerTokenInfosDto.ExternalInfo.ToDictionary(item => item.Key, item => item.Value);
        
        var inscriptionDeployMap = new Dictionary<string, string>();
        var inscriptionDeploy404Exists = externalInfo.TryGetValue("__inscription_deploy", out var inscriptionDeploy);
        var inscriptionDeployExists = externalInfo.TryGetValue("inscription_deploy", out var inscriptionDeployInfo);
        if (inscriptionDeploy404Exists)
        {
            inscriptionDeployMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(inscriptionDeploy);
        }
        else if (inscriptionDeployExists)
        {
            inscriptionDeployMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(inscriptionDeployInfo);
        }

        if (inscriptionDeployMap.TryGetValue("tick", out var inscriptionName))
        {
            item.InscriptionName = inscriptionName;
        }

        if (inscriptionDeployMap.TryGetValue("lim", out var lim))
        {
            item.LimitPerMint = lim;
        }


        if (externalInfo.TryGetValue("__seed_owned_symbol", out var seedOwnedSymbol))
        {
            item.SeedOwnedSymbol = seedOwnedSymbol;
        }

        if (externalInfo.TryGetValue("__seed_exp_time", out var seedExpTime))
        {
            item.Expires = seedExpTime;
        }

        if (externalInfo.TryGetValue("__inscription_adopt", out var inscriptionAdopt))
        {
            var inscriptionAdoptMap =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(inscriptionAdopt);
            if (inscriptionAdoptMap.TryGetValue("gen", out var gen))
            {
                item.Generation = gen;
            }

            if (inscriptionAdoptMap.TryGetValue("tick", out var tick))
            {
                item.InscriptionName = tick;
            }
        }

        if (externalInfo.TryGetValue("__nft_attributes", out var attributes))
        {
            item.Traits = attributes;
        }
    }
    
    private void CalculateAndSetTraitsPercentageAsync(List<NftItem> nftItems)
    {
        foreach (var item in nftItems.Where(item => !string.IsNullOrEmpty(item.Traits)))
        {
            item.TraitsPercentages = new List<Trait>();
        }
    }
    
    private async Task TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(List<NftItem> nftItems)
    {
        foreach (var item in nftItems)
        {
            await TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(item);
        }
    }

    private async Task TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(NftItem nftItem)
    {
        if (nftItem.IsSeed && (string.IsNullOrEmpty(nftItem.Expires) || string.IsNullOrEmpty(nftItem.SeedOwnedSymbol)))
        {
            //todo
            // var nftItemCache =
            //     await _tokenCacheProvider.GetTokenInfoAsync(nftItem.ChainId, nftItem.Symbol, TokenType.NFTItem);
            // nftItem.Expires = nftItemCache.Expires;
            // nftItem.SeedOwnedSymbol = nftItemCache.SeedOwnedSymbol;
        }
    }
    
    private void TryUpdateImageUrlForNftItems(List<NftItem> nftItems)
    {
        foreach (var nftItem in nftItems)
        {
            TryUpdateImageUrlForNftItem(nftItem);
        }
    }

    private void TryUpdateImageUrlForNftItem(NftItem nftItem)
    {
        nftItem.ImageUrl = IpfsImageUrlHelper.TryGetIpfsImageUrl(nftItem.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
        nftItem.ImageLargeUrl =
            IpfsImageUrlHelper.TryGetIpfsImageUrl(nftItem.ImageLargeUrl, _ipfsOptions?.ReplacedIpfsPrefix);
    }

    
    private void OptimizeSeedAliasDisplayForNftItems(List<NftItem> nftItems)
    {
        foreach (var item in nftItems)
        {
            OptimizeSeedAliasDisplayForNftItem(item);
        }
    }

    private void OptimizeSeedAliasDisplayForNftItem(NftItem nftItem)
    {
        if (nftItem.IsSeed && nftItem.Alias.EndsWith(TokensConstants.SeedAliasNameSuffix))
        {
            nftItem.Alias = nftItem.Alias.TrimEnd(TokensConstants.SeedAliasNameSuffix.ToCharArray());
        }
    }
    
    private void TryUpdateLimitPerMintForInscription(List<NftItem> nftItems)
    {
        foreach (var nftItem in nftItems)
        {
            TryUpdateLimitPerMintForInscription(nftItem);
        }
    }

    private void TryUpdateLimitPerMintForInscription(NftItem nftItem)
    {
        if (!string.IsNullOrEmpty(nftItem.LimitPerMint) && nftItem.LimitPerMint.Equals("0"))
        {
            nftItem.LimitPerMint = TokensConstants.LimitPerMintReplacement;
        }
    }
    
    private void SetSeedStatusAndTypeForNftItems(List<NftItem> nftItems)
    {
        foreach (var nftItem in nftItems)
        {
            SetSeedStatusAndTypeForNftItem(nftItem);
        }
    }
    
    private void SetSeedStatusAndTypeForNftItem(NftItem nftItem)
    {
        // If the Symbol starts with "SEED", we set IsSeed to true.
        if (nftItem.Symbol.StartsWith(TokensConstants.SeedNamePrefix))
        {
            nftItem.IsSeed = true;
            nftItem.SeedType = (int)SeedType.FT;

            if (!string.IsNullOrEmpty(nftItem.SeedOwnedSymbol))
            {
                nftItem.SeedType = nftItem.SeedOwnedSymbol.Contains("-") ? (int)SeedType.NFT : (int)SeedType.FT;
            }

            // Compatible with historical data
            // If the TokenName starts with "SEED-", we remove "SEED-" and check if it contains "-"
            else if (!string.IsNullOrEmpty(nftItem.TokenName) &&
                     nftItem.TokenName.StartsWith(TokensConstants.SeedNamePrefix))
            {
                var tokenNameWithoutSeed = nftItem.TokenName.Remove(0, 5);

                // If TokenName contains "-", set SeedType to NFT, otherwise set it to FT
                nftItem.SeedType = tokenNameWithoutSeed.Contains("-") ? (int)SeedType.NFT : (int)SeedType.FT;
            }
        }
    }

    private async Task<List<AddressNftInfoDto>> GetUserCollectionItemsAsync(List<AddressInfo> addressInfos, string symbol)
    {
        var nftList = new List<AddressNftInfoDto>();
        var url = _aelfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanUserNFTAssetsApi;

        foreach (var addressInfo in addressInfos)
        {
            // get all NFT for TotalNftItemCount
            var requestUrl = $"{url}?Address={addressInfo.Address}&ChainId={addressInfo.ChainId}&SkipCount=0&MaxResultCount={LimitedResultRequestDto.MaxMaxResultCount}";
            var chainTokenList = await _httpClientProvider.GetDataAsync<GetAddressNftListResultDto>(requestUrl);
            nftList.AddRange(chainTokenList.List);
        }

        return nftList.Where(t => t.NftCollection.Symbol == symbol).ToList();
    }
    
    private void TryUpdateImageUrlForCollections(List<NftCollection> collections)
    {
        foreach (var collection in collections)
        {
            collection.ImageUrl =
                IpfsImageUrlHelper.TryGetIpfsImageUrl(collection.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
        }
    }
    
    private static void DealWithDisplayChainImage(GetNftCollectionsDto dto)
    {
        var symbolToCount = dto.Data.GroupBy(nft => nft.Symbol)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionary(g => g.GroupId, g => g.Count);
        foreach (var nftCollection in dto.Data)
        {
            symbolToCount.TryGetValue(nftCollection.Symbol, out var count);
            nftCollection.DisplayChainImage = count > 1;
        }
    }
    
    private async Task<GetTokenDto> ConvertDtoAsync(GetAddressTokenListResultDto fromDto, GetTokenRequestDto requestDto)
    {
        var result = new GetTokenDto()
        {
            TotalBalanceInUsd = fromDto.AssetInUsd.ToString(),
            TotalRecordCount = fromDto.Total,
        };
        
        foreach (var fromTokenInfoDto in fromDto.List)
        {
            var tokenContractAddress = fromTokenInfoDto.ChainIds.Count > 0 && _chainOptions.ChainInfos.ContainsKey(fromTokenInfoDto.ChainIds[0])
                ? _chainOptions.ChainInfos[fromTokenInfoDto.ChainIds[0]].TokenContractAddress
                : null;
            
            var token = new Dtos.Token
            {
                ChainId = fromTokenInfoDto.ChainIds[0],
                Symbol = fromTokenInfoDto.Token.Symbol,
                Price = fromTokenInfoDto.PriceOfUsd,
                Balance = fromTokenInfoDto.Quantity.ToString(),
                Decimals = fromTokenInfoDto.Token.Decimals,
                BalanceInUsd = fromTokenInfoDto.ValueOfUsd.ToString(),
                TokenContractAddress = tokenContractAddress,
                ImageUrl = fromTokenInfoDto.Token.ImageUrl,
            };
            
            var resultTokenInfo = result.Data.FirstOrDefault(t => t.Symbol == fromTokenInfoDto.Token.Symbol);
            if (resultTokenInfo == null)
            {
                result.Data.Add(new TokenWithoutChain
                {
                    Symbol = fromTokenInfoDto.Token.Symbol,
                    Price = fromTokenInfoDto.PriceOfUsd,
                    Balance = fromTokenInfoDto.Quantity.ToString(),
                    Decimals = fromTokenInfoDto.Token.Decimals,
                    BalanceInUsd = fromTokenInfoDto.ValueOfUsd.ToString(),
                    TokenContractAddress = tokenContractAddress,
                    ImageUrl = fromTokenInfoDto.Token.ImageUrl,
                    Tokens = new List<Dtos.Token>()
                    {
                        token
                    }
                });
            }
            else
            {
                resultTokenInfo.Balance = (decimal.Parse(resultTokenInfo.Balance) + fromTokenInfoDto.Quantity).ToString();
                resultTokenInfo.BalanceInUsd = (decimal.Parse(resultTokenInfo.BalanceInUsd) + fromTokenInfoDto.ValueOfUsd).ToString();
                resultTokenInfo.Tokens.Add(token);
            }
        }

        foreach (var tokenWithoutChain in result.Data)
        {
            foreach (var addressInfo in requestDto.AddressInfos)
            {
                var chainTokenInfo = tokenWithoutChain.Tokens.FirstOrDefault(t => t.ChainId == addressInfo.ChainId);
                if (chainTokenInfo == null)
                {
                    tokenWithoutChain.Tokens.Add(new Dtos.Token
                    {
                        ChainId = addressInfo.ChainId,
                        Symbol = tokenWithoutChain.Symbol,
                        Price = 0,
                        Balance = "0",
                        Decimals = tokenWithoutChain.Decimals,
                        BalanceInUsd = "0",
                        TokenContractAddress = _chainOptions.ChainInfos[addressInfo.ChainId].TokenContractAddress,
                        ImageUrl = tokenWithoutChain.ImageUrl
                    });
                }
            }

            tokenWithoutChain.Tokens.OrderByDescending(t => t.ChainId);
        }

        result.TotalDisplayCount = result.Data.Select(item => item.Tokens.Count).Sum();
        
        return result;
    }
    
    private void AddDefaultTokens(GetAddressTokenListResultDto tokensResultDto)
    {
        foreach (var item in _tokenListOptions.UserToken)
        {
            var token = tokensResultDto.List.FirstOrDefault(t =>
                t.ChainIds[0] == item.Token.ChainId && t.Token.Symbol == item.Token.Symbol);
            if (token != null || (!item.IsDefault && !item.IsDisplay))
            {
                continue;
            }

            tokensResultDto.List.Add(new TokenInfoDto
            {
                Token = new TokenBaseInfo
                {
                    Symbol = item.Token.Symbol,
                    Decimals = item.Token.Decimals
                },
                ChainIds = new List<string>()
                {
                    item.Token.ChainId
                },
                Type = SymbolType.Token
            });
        }
    }
}