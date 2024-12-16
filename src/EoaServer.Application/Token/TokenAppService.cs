using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Entities.Es;
using EoaServer.Options;
using EoaServer.Token.Dto;
using EoaServer.Token.Request;
using EoaServer.Token.TokenPrice;
using EoaServer.UserAssets.Provider;
using EoaServer.UserToken;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace EoaServer.Token;

[RemoteService(false)]
[DisableAuditing]
public class TokenAppService : EoaServerBaseService, ITokenAppService
{
    private readonly ILogger<TokenAppService> _logger;
    private readonly ActivityOptions _activityOptions;
    private readonly TokenSpenderOptions _tokenSpenderOptions;
    private readonly ChainOptions _chainOptions;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly IAElfScanDataProvider _aelfScanDataProvider;
    private readonly IUserTokenProvider _userTokenProvider;
    private readonly NftToFtOptions _nftToFtOptions;
    private readonly TokenListOptions _tokenListOptions;
    private readonly ITokenPriceService _tokenPriceService;

    public TokenAppService(ILogger<TokenAppService> logger,
        IOptionsSnapshot<ActivityOptions> activityOptions,
        IOptionsSnapshot<TokenSpenderOptions> tokenSpenderOptions,
        IOptionsSnapshot<ChainOptions> chainOptions,
        ITokenInfoProvider tokenInfoProvider,
        IAElfScanDataProvider aelfScanDataProvider,
        IUserTokenProvider userTokenProvider,
        IOptionsSnapshot<NftToFtOptions> nftToFtOptions,
        IOptionsSnapshot<TokenListOptions> tokenListOptions,
        ITokenPriceService tokenPriceService)
    {
        _logger = logger;
        _activityOptions = activityOptions.Value;
        _tokenSpenderOptions = tokenSpenderOptions.Value;
        _chainOptions = chainOptions.Value;
        _tokenInfoProvider = tokenInfoProvider;
        _aelfScanDataProvider = aelfScanDataProvider;
        _userTokenProvider = userTokenProvider;
        _tokenPriceService = tokenPriceService;
        _nftToFtOptions = nftToFtOptions.Value;
        _tokenListOptions = tokenListOptions.Value;
    }

    private void AddDefaultTokens(List<UserTokenIndex> tokens, string keyword)
    {
        var userTokens = _tokenListOptions.UserToken;
        if (!string.IsNullOrEmpty(keyword))
        {
            userTokens =
                userTokens.Where(t => t.Token.Symbol.ToUpper().Contains(keyword.Trim().ToUpper())).ToList();
        }

        foreach (var item in userTokens)
        {
            var token = tokens.FirstOrDefault(t =>
                t.Token.ChainId == item.Token.ChainId && t.Token.Symbol == item.Token.Symbol);
            if (token != null)
            {
                continue;
            }

            tokens.Add(ObjectMapper.Map<UserTokenItem, UserTokenIndex>(item));
        }
    }

    private List<GetTokenListDto> Convert(List<TokenCommonDto> tokenInfos, List<UserTokenIndex> userTokenInfos)
    {
        var result = new List<GetTokenListDto>();
        var tokenList = new List<GetTokenListDto>();
        foreach (var tokenCommonDto in tokenInfos)
        {
            var tokenDto = new GetTokenListDto
            {
                ChainId = tokenCommonDto.ChainIds[0],
                Id = _tokenInfoProvider.GetTokenId(tokenCommonDto.ChainIds[0], tokenCommonDto.Token.Symbol),
                Symbol = tokenCommonDto.Token.Symbol,
                Decimals = tokenCommonDto.Token.Decimals,
                TokenName = tokenCommonDto.Token.Name,
                ImageUrl = tokenCommonDto.Token.ImageUrl
            };
            tokenList.Add(tokenDto);
        }

        var userTokens = ObjectMapper.Map<List<UserTokenIndex>, List<GetTokenListDto>>(userTokenInfos);
        if (tokenList.Count > 0)
        {
            tokenList.RemoveAll(t =>
                userTokens.Select(f => new { f.Symbol, f.ChainId }).Contains(new { t.Symbol, t.ChainId }));
        }

        if (userTokens.Select(t => t.IsDefault).Contains(true))
        {
            result.AddRange(userTokens.Where(t => t.IsDefault).OrderBy(t => t.ChainId));
            userTokens.RemoveAll(t => t.IsDefault);
        }

        if (userTokens.Select(t => t.IsDisplay).Contains(true))
        {
            result.AddRange(userTokens.Where(t => t.IsDisplay).OrderBy(t => t.Symbol).ThenBy(t => t.ChainId));
            userTokens.RemoveAll(t => t.IsDisplay);
        }

        userTokens.AddRange(tokenList);
        result.AddRange(userTokens.OrderBy(t => t.Symbol).ThenBy(t => t.ChainId).ToList());

        return result;
    }

    public async Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input)
    {
        var indexerTokens = new List<TokenCommonDto>();
        foreach (var chainId in input.ChainIds)
        {
            var chainIndexerToken =
                await _aelfScanDataProvider.GetTokenListAsync(chainId, input.Symbol);
            indexerTokens.AddRange(chainIndexerToken.List);
        }

        var userTokensDto = await _userTokenProvider.GetUserTokenInfoListAsync(
            CurrentUser.GetId(),
            input.ChainIds.Count == 1 ? input.ChainIds.First() : string.Empty,
            string.Empty);

        AddDefaultTokens(userTokensDto, input.Symbol);
        userTokensDto = userTokensDto?.Where(t => t.Token.Symbol.Contains(input.Symbol.Trim().ToUpper())).ToList();


        var tokenInfoList = Convert(indexerTokens, userTokensDto);

        // Check and adjust SkipCount and MaxResultCount
        var skipCount = input.SkipCount < TokensConstants.SkipCountDefault
            ? TokensConstants.SkipCountDefault
            : input.SkipCount;
        var maxResultCount = input.MaxResultCount <= TokensConstants.MaxResultCountInvalid
            ? TokensConstants.MaxResultCountDefault
            : input.MaxResultCount;

        tokenInfoList = tokenInfoList.Skip(skipCount).Take(maxResultCount).ToList();
        foreach (var token in tokenInfoList)
        {
            token.ImageUrl = _tokenInfoProvider.BuildSymbolImageUrl(token.Symbol);
        }

        foreach (var nffItem in tokenInfoList.Where(t => _nftToFtOptions.NftToFtInfos.Keys.Contains(t.Symbol)))
        {
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(nffItem.Symbol);
            if (nftToFtInfo != null)
            {
                nffItem.Label = nftToFtInfo.Label;
                nffItem.ImageUrl = nftToFtInfo.ImageUrl;
            }
        }

        return tokenInfoList;
    }

    public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
    {
        var result = new List<TokenPriceDataDto>();
        if (symbols.Count == 0)
        {
            return new ListResultDto<TokenPriceDataDto>();
        }

        var symbolList = symbols.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
        foreach (var symbol in symbolList)
        {
            var priceResult = await _tokenPriceService.GetCurrentPriceAsync(symbol);
            result.Add(priceResult);
        }

        return new ListResultDto<TokenPriceDataDto>
        {
            Items = result
        };
    }
}