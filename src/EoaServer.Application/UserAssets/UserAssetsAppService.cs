using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token.Dto;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using TokenInfoDto = EoaServer.UserAssets.Dtos.TokenInfoDto;

namespace EoaServer.UserAssets;


[RemoteService(false)]
[DisableAuditing]
public class UserAssetsAppService : EoaServerBaseService, IUserAssetsAppService
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly AElfScanOptions _aElfScanOptions;
    private readonly TokenListOptions _tokenListOptions;

    public UserAssetsAppService(IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<AElfScanOptions> aElfScanOptions,
        IOptionsSnapshot<TokenListOptions> tokenListOptions)
    {
        _httpClientProvider = httpClientProvider;
        _aElfScanOptions = aElfScanOptions.Value;
        _tokenListOptions = tokenListOptions.Value;
    }
    
    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        var tokenList = new GetAddressTokenListResultDto();
        var url = _aElfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanUserTokenAssetsApi;

        foreach (var addressInfo in requestDto.AddressInfos)
        {
            var requestUrl = $"{url}?Address={addressInfo.Address}&ChainId={addressInfo.ChainId}&SkipCount={requestDto.SkipCount}&MaxResultCount={requestDto.MaxResultCount}";
            var chainTokenList = await _httpClientProvider.GetDataAsync<GetAddressTokenListResultDto>(requestUrl);
            tokenList.AssetInUsd += chainTokenList.AssetInUsd;
            tokenList.AssetInElf += chainTokenList.AssetInElf;
            tokenList.Total += chainTokenList.Total;
            tokenList.List.AddRange(chainTokenList.List);
        }
        
        AddDefaultTokens(tokenList);
        
        var result = await ConvertDtoAsync(tokenList);
        
        return result;
    }
    
    public async Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto)
    {
        //todo
        return null;
    }

    public async Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        //todo
        return null;
    }

    private async Task<GetTokenDto> ConvertDtoAsync(GetAddressTokenListResultDto dto)
    {
        var result = new GetTokenDto()
        {
            TotalBalanceInUsd = dto.AssetInUsd.ToString(),
            TotalRecordCount = dto.Total,
            TotalDisplayCount = 0 // todo
        };
        
        foreach (var tokenInfoDto in dto.List)
        {
            var chainTokenInfo = new Dtos.Token
            {
                ChainId = tokenInfoDto.ChainIds[0],
                Symbol = tokenInfoDto.Token.Symbol,
                Price = tokenInfoDto.PriceOfUsd,
                Balance = tokenInfoDto.Quantity.ToString(),
                Decimals = tokenInfoDto.Token.Decimals,
                BalanceInUsd = tokenInfoDto.ValueOfUsd.ToString(),
                TokenContractAddress = null, //todo
                ImageUrl = tokenInfoDto.Token.ImageUrl,
                Label = null,
                DisplayChainName = null, //todo
                ChainImageUrl = null //todo
            };
            
            var resultTokenInfo = result.Data.FirstOrDefault(t => t.Symbol == tokenInfoDto.Token.Symbol);
            if (resultTokenInfo == null)
            {
                result.Data.Add(new TokenWithoutChain
                {
                    Symbol = tokenInfoDto.Token.Symbol,
                    Price = tokenInfoDto.PriceOfUsd,
                    Balance = tokenInfoDto.Quantity.ToString(),
                    Decimals = tokenInfoDto.Token.Decimals,
                    BalanceInUsd = tokenInfoDto.ValueOfUsd.ToString(),
                    TokenContractAddress = null, //todo
                    ImageUrl = tokenInfoDto.Token.ImageUrl,
                    Label = null,
                    Tokens = new List<Dtos.Token>()
                    {
                        chainTokenInfo
                    }
                });
            }
            else
            {
                resultTokenInfo.Balance = (decimal.Parse(resultTokenInfo.Balance) + tokenInfoDto.Quantity).ToString();
                resultTokenInfo.BalanceInUsd = (decimal.Parse(resultTokenInfo.BalanceInUsd) + tokenInfoDto.ValueOfUsd).ToString();
                resultTokenInfo.Tokens.Add(chainTokenInfo);
            }
        }

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