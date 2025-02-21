using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EoaServer.Awaken;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token;
using EoaServer.Token.Dto;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dto;
using EoaServer.UserAssets.Dtos;
using EoaServer.UserAssets.Provider;
using EoaServer.UserToken;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Users;
using TokenInfoDto = EoaServer.UserAssets.Dtos.TokenInfoDto;

namespace EoaServer.UserAssets;


[RemoteService(false)]
[DisableAuditing]
public class UserAssetsAppService : EoaServerBaseService, IUserAssetsAppService
{
    private readonly TokenListOptions _tokenListOptions;
    private readonly SeedImageOptions _seedImageOptions;
    private readonly IImageProcessProvider _imageProcessProvider;
    private readonly IpfsOptions _ipfsOptions;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly NftItemDisplayOption _nftItemDisplayOption;
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<UserAssetsAppService> _logger;
    private readonly IAElfScanDataProvider _aelfScanDataProvider;
    private readonly IUserTokenProvider _userTokenProvider;
    private readonly AwakenOptions _awakenOptions;
    private readonly IHttpClientService _httpClientService;
    private readonly TokenInfoOptions _tokenInfoOptions;
    private readonly NftToFtOptions _nftToFtOptions;

    public UserAssetsAppService(
        IOptionsSnapshot<TokenListOptions> tokenListOptions,
        IOptionsSnapshot<SeedImageOptions> seedImageOptions,
        IImageProcessProvider imageProcessProvider,
        IOptionsSnapshot<IpfsOptions> ipfsOption,
        IOptionsSnapshot<ChainOptions> chainOptions,
        ITokenInfoProvider tokenInfoProvider,
        ILogger<UserAssetsAppService> logger,
        IOptionsSnapshot<NftItemDisplayOption> nftItemDisplayOptions,
        IAElfScanDataProvider aelfScanDataProvider,
        IUserTokenProvider userTokenProvider,
        IOptionsSnapshot<AwakenOptions> awakenOptions,
        IHttpClientService httpClientService,
        IOptionsSnapshot<TokenInfoOptions> tokenInfoOptions,
        IOptionsSnapshot<NftToFtOptions> nftToFtOptions)
    {
        _tokenListOptions = tokenListOptions.Value;
        _seedImageOptions = seedImageOptions.Value;
        _imageProcessProvider = imageProcessProvider;
        _ipfsOptions = ipfsOption.Value;
        _tokenInfoProvider = tokenInfoProvider;
        _chainOptions = chainOptions.Value;
        _logger = logger;
        _nftItemDisplayOption = nftItemDisplayOptions.Value;
        _aelfScanDataProvider = aelfScanDataProvider;
        _userTokenProvider = userTokenProvider;
        _awakenOptions = awakenOptions.Value;
        _httpClientService = httpClientService;
        _tokenInfoOptions = tokenInfoOptions.Value;
        _nftToFtOptions = nftToFtOptions.Value;
    }

    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        return await GetTokenAsync(requestDto, true);
    }

    public async Task<GetTokenDto> GetTokenAsync(GetAssetsBase requestDto, bool addDefaultToken)
    {
        var tokenList = new GetAddressTokenListResultDto();
        foreach (var addressInfo in requestDto.AddressInfos)
        {
            var chainTokenList = await _aelfScanDataProvider.GetAddressTokenAssetsAsync(addressInfo.ChainId, addressInfo.Address);
            if (chainTokenList == null || chainTokenList.List.IsNullOrEmpty()) continue;
            tokenList.AssetInUsd += chainTokenList.AssetInUsd;
            tokenList.AssetInElf += chainTokenList.AssetInElf;
            tokenList.Total += chainTokenList.Total;
            tokenList.List.AddRange(chainTokenList.List);
        }

        var tokenInfoMap = await _tokenInfoProvider.GetTokenMapAsync(tokenList.List.Select( t=> t.Token.Symbol).ToHashSet());
        foreach (var chainToken in tokenList.List)
        {
            if (!tokenInfoMap.TryGetValue(chainToken.Token.Symbol, out var tokenInfoDto))
            {
                continue;
            }

            chainToken.Token.Decimals = tokenInfoDto.Decimals;
            chainToken.Quantity = (long) ((double) chainToken.Quantity * Math.Pow(10, tokenInfoDto.Decimals));
        }

        if (addDefaultToken)
        {
            AddDefaultTokens(tokenList);
        }
        var result = await ConvertDtoAsync(tokenList, requestDto);
        
        result.Data = SortTokens(result.Data);
        result.TotalRecordCount = result.Data.Count;
        result.Data = result.Data.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();
        
        return result;
    }
    
    private List<TokenWithoutChain> SortTokens(List<TokenWithoutChain> tokens)
    {
        foreach (var tokenWithoutChain in tokens)
        {
            tokenWithoutChain.Tokens = tokenWithoutChain.Tokens.OrderByDescending(t => t.ChainId).ToList();
        }
        
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
        var result = new GetNftCollectionsDto();
        var nftList = new GetAddressNftListResultDto();
        

        foreach (var addressInfo in requestDto.AddressInfos)
        {
            var chainTokenList = await _aelfScanDataProvider.GetAddressNftListAsync(addressInfo.ChainId, addressInfo.Address);
            if (chainTokenList != null && !chainTokenList.List.IsNullOrEmpty())
            {
                nftList.Total += chainTokenList.Total;
                nftList.List.AddRange(chainTokenList.List);
            }
            else
            {
                _logger.LogError($"Get Nft list result is null. chainId: {addressInfo.ChainId}, address: {addressInfo.Address}");
            }
        }
        
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

        result.TotalNftItemCount = nftList.List.Count;
        result.TotalRecordCount = result.Data.Count();
        
        result.Data = result.Data.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();
        
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
            return await _aelfScanDataProvider.GetIndexerTokenInfoAsync(nftItem.ChainIds[0], nftItem.Token.Symbol);
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

        foreach (var addressInfo in addressInfos)
        {
            // get all NFT for TotalNftItemCount
            var chainTokenList = await _aelfScanDataProvider.GetAddressNftListAsync(addressInfo.ChainId, addressInfo.Address);
            if (chainTokenList != null && !chainTokenList.List.IsNullOrEmpty())
            {
                nftList.AddRange(chainTokenList.List);
            }
            else
            {
                _logger.LogError($"Get Nft list result is null. chainId: {addressInfo.ChainId}, address: {addressInfo.Address}");
            }
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
    
    private async Task<GetTokenDto> ConvertDtoAsync(GetAddressTokenListResultDto fromDto, GetAssetsBase requestDto)
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
            
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(fromTokenInfoDto.Token.Symbol);
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
            if (nftToFtInfo != null)
            {
                token.Label = nftToFtInfo.Label;
                token.ImageUrl = nftToFtInfo.ImageUrl;
            }
            
            
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
                    ImageUrl = token.ImageUrl,
                    Label = token.Label,
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
                        ImageUrl = tokenWithoutChain.ImageUrl,
                        Label = tokenWithoutChain.Label
                    });
                }
            }
        }

        result.TotalDisplayCount = result.Data.Select(item => item.Tokens.Count).Sum();
        
        return result;
    }

    private async Task AddUserTokensAsync(GetAddressTokenListResultDto tokensResultDto)
    {
        if (!CurrentUser.Id.HasValue)
        {
            return;
        }
        var userId = CurrentUser.GetId();
        var userTokens =
            await _userTokenProvider.GetUserTokenInfoListAsync(userId, string.Empty, string.Empty);
        foreach (var userToken in userTokens)
        {
            var resultToken = tokensResultDto.List.FirstOrDefault(t =>
                t.Token.Symbol == userToken.Token.Symbol && t.ChainIds[0] == userToken.Token.ChainId);
            if (resultToken == null)
            {
                tokensResultDto.List.Add(new TokenInfoDto
                {
                    Token = new TokenBaseInfo()
                    {
                        Decimals = userToken.Token.Decimals,
                        Symbol = userToken.Token.Symbol,
                        ImageUrl = _tokenInfoProvider.BuildSymbolImageUrl(userToken.Token.Symbol)
                    },
                    ChainIds = new List<string>()
                    {
                        userToken.Token.ChainId
                    },
                    Type = SymbolType.Token
                });
            }
        }
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
                    Decimals = item.Token.Decimals,
                    ImageUrl = _tokenInfoProvider.BuildSymbolImageUrl(item.Token.Symbol)
                },
                ChainIds = new List<string>()
                {
                    item.Token.ChainId
                },
                Type = SymbolType.Token
            });
        }
    }

    public async Task<AwakenSupportedTokenResponse> ListAwakenSupportedTokensAsync(int skipCount, int maxResultCount, int page, string chainId, string caAddress)
    {
        var awakenUrl = _awakenOptions.Domain +
                        $"/api/app/trade-pairs?skipCount={skipCount}&maxResultCount={maxResultCount}&page={page}&chainId={chainId}";
        var response = await _httpClientService.GetAsync<CommonResponseDto<TradePairsDto>>(awakenUrl);
        if (!response.Success || response.Data == null || response.Data.Items.IsNullOrEmpty())
        {
            return new AwakenSupportedTokenResponse()
            {
                Total = 0,
                Data = new List<Dtos.Token>()
            };
        }

        var tokens0 = response.Data.Items.Select(item => item.Token0).Distinct(new TokenComparer()).ToList();
        var tokens1 = response.Data.Items.Select(item => item.Token1).Distinct(new TokenComparer()).ToList();
        tokens0.AddRange(tokens1);
        var tokens = tokens0.Distinct(new TokenComparer()).ToList();
        var result = ObjectMapper.Map<List<TradePairsItemToken>, List<UserAssets.Dtos.Token>>(tokens);
        var symbolToToken = await ListSideChainUserTokens(chainId, caAddress, tokens);
        var tokenImageDic = _tokenInfoOptions.TokenInfos.ToDictionary(k => k.Key, v => v.Value.ImageUrl);
        foreach (var token in result)
        {
            ChainDisplayNameHelper.SetDisplayName(token);
            if (!symbolToToken.TryGetValue(token.Symbol, out var userToken))
            {
                token.Balance = "0";
                token.BalanceInUsd = "0";
            }
            else
            {
                token.Balance = userToken.Balance;
                token.BalanceInUsd = userToken.BalanceInUsd;
                token.Price = token.Price == 0 ? userToken.Price : token.Price;
            }

            token.ImageUrl = _tokenInfoProvider.BuildSymbolImageUrl(token.Symbol);
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(token.Symbol);
            if (nftToFtInfo != null)
            {
                token.Label = nftToFtInfo.Label;
                token.ImageUrl = nftToFtInfo.ImageUrl;
            }
        }

        result = SortTokens(result);
        result = result.Skip(skipCount).Take(maxResultCount).ToList();
        return new AwakenSupportedTokenResponse()
        {
            Total = result.Count,
            Data = result
        };
    }
    
    private async Task<Dictionary<string, Dtos.Token>> ListSideChainUserTokens(string chainId, string address,
        List<TradePairsItemToken> tokens)
    {
        var userTokens = new List<Dtos.Token>();
        var userTokenInfos = await _aelfScanDataProvider.GetAddressTokenAssetsAsync(chainId, address);
        if (userTokenInfos == null || userTokenInfos.List.IsNullOrEmpty())
        {
            return new Dictionary<string, Dtos.Token>();
        }

        var symbols = tokens.Select(t => t.Symbol).Distinct().ToList();
        foreach (var tokenInfoDto in userTokenInfos.List)
        {
            if (!symbols.Contains(tokenInfoDto.Token.Symbol))
            {
                continue;
            }

            var awakenToken = tokens.First(t => t.Symbol == tokenInfoDto.Token.Symbol);
            var token = new Dtos.Token()
            {
                Decimals = awakenToken.Decimals,
                Symbol = tokenInfoDto.Token.Symbol,
                Balance = ((long)((double)tokenInfoDto.Quantity * Math.Pow(10, awakenToken.Decimals))).ToString(),
                BalanceInUsd = tokenInfoDto.ValueOfUsd.ToString(),
                ChainId = chainId,
                Price = tokenInfoDto.PriceOfUsd,
            };
            userTokens.Add(token);
        }
        
        try
        {
            return userTokens.ToDictionary(token => token.Symbol, token => token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "sideChainUserTokens.ToDictionary error");
            return new Dictionary<string, Dtos.Token>();
        }
    }

    private List<Dtos.Token> SortTokens(List<Dtos.Token> tokens)
    {
        var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();

        try
        {
            return tokens.OrderBy(t => decimal.Parse(t.Balance) == 0)
                .ThenBy(t => t.Symbol != CommonConstant.ELF)
                .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
                .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
                .ThenBy(t => t.Symbol)
                .ThenBy(t => t.ChainId)
                .ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "illegal tokens:{0}",
                JsonConvert.SerializeObject(tokens.Where(t => t.Balance.IsNullOrEmpty()).ToList()));
            return tokens;
        }
    }

    public async Task<SearchUserAssetsV2Dto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto)
    {
        var result = new SearchUserAssetsV2Dto();
        var getTokenDto = await GetTokenAsync(requestDto, false);
        foreach (var tokenInfo in getTokenDto.Data)
        {
            if (!requestDto.Keyword.IsNullOrWhiteSpace() && !tokenInfo.Symbol.Contains(requestDto.Keyword))
            {
                continue;
            }
            var tokenInfoDtoList = ObjectMapper.Map<List<Dtos.Token>, List<TokenInfoV2Dto>>(tokenInfo.Tokens);
            result.TokenInfos.AddRange(tokenInfoDtoList);
        }
        result.TokenInfos.ForEach(t => t.Address = requestDto.AddressInfos[0].Address);
        result.TokenInfos = result.TokenInfos.Where(t => t.Balance != "0").ToList();
        

        var collectionDto = await GetNFTCollectionsAsync(new GetNftCollectionsRequestDto()
        {
            SkipCount = requestDto.SkipCount,
            MaxResultCount = requestDto.MaxResultCount,
            AddressInfos = requestDto.AddressInfos,
            Height = requestDto.Height,
            Width = requestDto.Width
        });
        var nftItemsTask = collectionDto.Data.Select(t => GetNFTItemsAsync(new GetNftItemsRequestDto()
        {
            SkipCount = requestDto.SkipCount,
            MaxResultCount = requestDto.MaxResultCount,
            AddressInfos = requestDto.AddressInfos.Where(a => a.ChainId == t.ChainId).ToList(),
            Height = requestDto.Height,
            Width = requestDto.Width,
            Symbol = t.Symbol
        }));
        var nftItemsDtoList = (await Task.WhenAll(nftItemsTask)).Where(t => t.Data.Count > 0).ToList();
        var nftItemsMap = nftItemsDtoList.ToDictionary(t => t.Data[0].CollectionSymbol + "-" + t.Data[0].ChainId, t => t);
        foreach (var collection in collectionDto.Data)
        {
            var collectionInfo = result.NftInfos.FirstOrDefault(t => t.CollectionName == collection.CollectionName);
            var isNewCollection = false;
            if (collectionInfo == null)
            {
                isNewCollection = true;
                collectionInfo = new NftCollectionDto();
            }
            collectionInfo.CollectionName = collection.CollectionName;
            collectionInfo.ImageUrl = collection.ImageUrl;
            if (!nftItemsMap.TryGetValue(collection.Symbol + "-" + collection.ChainId, out var nftItems) || nftItems.Data.IsNullOrEmpty())
            {
                continue;
            }
            foreach (var nftItem in nftItems.Data)
            {
                if (!requestDto.Keyword.IsNullOrWhiteSpace() && !nftItem.Symbol.Contains(requestDto.Keyword))
                {
                    continue;
                }
                var nftItemInfo = ObjectMapper.Map<NftItem, NftInfoDto>(nftItem);
                nftItemInfo.CollectionName = collection.CollectionName;
                var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(nftItem.Symbol);
                if (nftToFtInfo != null)
                {
                    nftItemInfo.Label = nftToFtInfo.Label;
                    nftItemInfo.ImageUrl = nftToFtInfo.ImageUrl;
                }
                collectionInfo.Items.Add(nftItemInfo);
            }

            if (collectionInfo.Items.Count > 0 && isNewCollection)
            {
                result.NftInfos.Add(collectionInfo);
            }
        }

        result.TotalRecordCount = result.TokenInfos.Count + result.NftInfos.Sum(t => t.Items.Count);
        return result;
    }
}