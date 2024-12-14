using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Entities.Es;
using EoaServer.Grain.UserToken;
using EoaServer.Options;
using EoaServer.Token;
using EoaServer.Token.Dto;
using EoaServer.Token.Eto;
using EoaServer.Token.Request;
using EoaServer.UserAssets.Provider;
using EoaServer.UserToken.Dto;
using EoaServer.UserToken.Request;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace EoaServer.UserToken;


[RemoteService(false)]
[DisableAuditing]
public class UserTokenAppService : EoaServerBaseService, IUserTokenAppService
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly AElfScanOptions _aelfScanOptions;
    private readonly TokenListOptions _tokenListOptions;
    private readonly SeedImageOptions _seedImageOptions;
    private readonly IImageProcessProvider _imageProcessProvider;
    private readonly IpfsOptions _ipfsOptions;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly NftItemDisplayOption _nftItemDisplayOption;
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<UserTokenAppService> _logger;
    private readonly IAElfScanDataProvider _aelfScanDataProvider;
    private readonly IUserTokenProvider _userTokenProvider;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;
    private readonly NftToFtOptions _nftToFtOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;

    public UserTokenAppService(IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<AElfScanOptions> aElfScanOptions,
        IOptionsSnapshot<TokenListOptions> tokenListOptions,
        IOptionsSnapshot<SeedImageOptions> seedImageOptions,
        IImageProcessProvider imageProcessProvider,
        IOptionsSnapshot<IpfsOptions> ipfsOption,
        IOptionsSnapshot<ChainOptions> chainOptions,
        ITokenInfoProvider tokenInfoProvider,
        ILogger<UserTokenAppService> logger,
        IOptionsSnapshot<NftItemDisplayOption> nftItemDisplayOptions,
        IAElfScanDataProvider aelfScanDataProvider,
        IUserTokenProvider userTokenProvider,
        IAssetsLibraryProvider assetsLibraryProvider,
        IOptionsSnapshot<NftToFtOptions> nftToFtOptions,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus)
    {
        _httpClientProvider = httpClientProvider;
        _aelfScanOptions = aElfScanOptions.Value;
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
        _nftToFtOptions = nftToFtOptions.Value;
        _assetsLibraryProvider = assetsLibraryProvider;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }
    
    public async Task ChangeTokenDisplayAsync(string id, bool isDisplay)
    {
        var (chainId, symbol) = id.Split('-') switch
        {
            var parts when parts.Length == 2 => (parts[0], parts[1]),
            _ => (string.Empty, string.Empty)
        };
        var userId = CurrentUser.GetId();
        var grainId = GrainIdHelper.GenerateGrainId(id, userId);
        var grain = _clusterClient.GetGrain<IUserTokenGrain>(grainId);
        var userTokenGrainResultDto = await grain.GetAsync();
        if (!userTokenGrainResultDto.Success())
        {
            var tokenInfo = await _tokenInfoProvider.GetAsync(chainId, symbol);
            var addResult = await grain.AddAsync(userId, new UserTokenGrainDto
            {
                UserId = userId,
                SortWeight = 0,
                Token = new Dto.Token()
                {
                    Id = id,
                    ChainId = chainId,
                    Address = tokenInfo.Address,
                    Symbol = symbol,
                    Decimals = tokenInfo.Decimals
                }
            });
            _logger.LogInformation($"Add user token: {JsonConvert.SerializeObject(addResult)}");
        }
        
        var tokenResult = await grain.ChangeDisplayAsync(userId, isDisplay, false);
        _logger.LogInformation($"Change user token: {tokenResult}");

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<UserTokenGrainDto, UserTokenEto>(tokenResult.Data));
    }
    
    public async Task<PagedResultDto<GetUserTokenDto>> GetTokensAsync(GetTokenInfosRequestDto requestDto)
    {
        var userId = CurrentUser.GetId();
        var userTokens =
            await _userTokenProvider.GetUserTokenInfoListAsync(userId, string.Empty, string.Empty);

        var sourceSymbols = _tokenListOptions.SourceToken.Select(t => t.Token.Symbol).Distinct().ToList();
        // hide source tokens.
        userTokens.RemoveAll(t => sourceSymbols.Contains(t.Token.Symbol) && !t.IsDisplay);

        var tokens = ObjectMapper.Map<List<UserTokenIndex>, List<GetUserTokenDto>>(userTokens);
        foreach (var item in _tokenListOptions.UserToken)
        {
            var token = tokens.FirstOrDefault(t =>
                t.ChainId == item.Token.ChainId && t.Symbol == item.Token.Symbol);
            if (token != null)
            {
                continue;
            }

            tokens.Add(ObjectMapper.Map<UserTokenItem, GetUserTokenDto>(item));
        }

        if (!string.IsNullOrEmpty(requestDto.Keyword))
        {
            tokens = tokens.Where(t => t.Symbol.Trim().ToUpper().Contains(requestDto.Keyword.ToUpper())).ToList();
        }

        if (!requestDto.ChainIds.IsNullOrEmpty())
        {
            tokens = tokens.Where(t => requestDto.ChainIds.Contains(t.ChainId)).ToList();
        }

        foreach (var token in tokens)
        {
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(token.Symbol);
            if (nftToFtInfo != null)
            {
                token.Label = nftToFtInfo.Label;
                token.ImageUrl = nftToFtInfo.ImageUrl;
                continue;
            }

            token.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(token.Symbol);
        }

        var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();
        tokens = tokens.OrderBy(t => t.Symbol != CommonConstant.ELF)
            //.ThenBy(t => !t.IsDisplay)
            .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
            .ThenBy(t => sourceSymbols.Contains(t.Symbol))
            .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
            .ThenBy(t => t.Symbol)
            .ThenByDescending(t => t.ChainId)
            .ToList();

        return new PagedResultDto<GetUserTokenDto>(tokens.Count,
            tokens.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList());
    }

    
}