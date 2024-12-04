using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token.Dto;
using EoaServer.UserActivity.Dto;
using EoaServer.UserActivity.Dtos;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace EoaServer.UserActivity;

[RemoteService(false)]
[DisableAuditing]
public class UserActivityAppService : EoaServerBaseService, IUserActivityAppService
{
    private readonly ILogger<UserActivityAppService> _logger;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ActivityOptions _activityOptions;
    private readonly TokenSpenderOptions _tokenSpenderOptions;
    private readonly ChainOptions _chainOptions;
    private readonly AElfScanOptions _aElfScanOptions;
    
    public UserActivityAppService(ILogger<UserActivityAppService> logger,
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<ActivityOptions> activityOptions,
        IOptionsSnapshot<TokenSpenderOptions> tokenSpenderOptions,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<AElfScanOptions> aElfScanOptions)
    {
        _logger = logger;
        _httpClientProvider = httpClientProvider;
        _activityOptions = activityOptions.Value;
        _tokenSpenderOptions = tokenSpenderOptions.Value;
        _chainOptions = chainOptions.Value;
        _aElfScanOptions = aElfScanOptions.Value;
    }
    
    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request)
    {
        var url = _aElfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTransactionDetailApi;
        var requestParams = new Dictionary<string, string>()
        {
            {"TransactionId", request.TransactionId},
            {"ChainId", request.ChainId}
        };
        var txnDto = await _httpClientProvider.GetAsync<TransactionDetailResponseDto>(url, requestParams);

        if (txnDto.List.Count < 1)
        {
            return null;
        }

        var tokenMap = await GetTokenMapAsync(txnDto);
        return await ConvertDtoAsync(request.ChainId, txnDto.List[0], tokenMap);
    }
    
    public async Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request)
    {
        //todo
        //foreach txn list txnId + chainIds[0] to get txn detail
        return null;
    }

    private async Task<Dictionary<string, IndexerTokenInfoDto>> GetTokenMapAsync(TransactionDetailResponseDto txnResponseDto)
    {
        var result = new Dictionary<string, IndexerTokenInfoDto>();
        foreach (var transactionDetail in txnResponseDto.List)
        {
            foreach (var tokenTransferred in transactionDetail.TokenTransferreds)
            {
                if (!result.ContainsKey(tokenTransferred.Symbol))
                {
                    result[tokenTransferred.Symbol] = null;
                }
            }
            foreach (var nftsTransferred in transactionDetail.NftsTransferreds)
            {
                if (!result.ContainsKey(nftsTransferred.Symbol))
                {
                    result[nftsTransferred.Symbol] = null;
                }
            }
        }

        var url = _aElfScanOptions.BaseUrl + "/" + CommonConstant.AelfScanTokenInfoApi;
        var sideChain = _chainOptions.ChainInfos.FirstOrDefault(t => t.Value.IsMainChain == false);
        var tokenChain = sideChain.Value.ChainId;
        
        var mapTasks = result.Select(async token =>
        {
            var requestParams = new Dictionary<string, string>()
            {
                { "Symbol", token.Key },
                { "ChainId", tokenChain }
            };
            var tokenInfo = await _httpClientProvider.GetAsync<IndexerTokenInfoDto>(url, requestParams);
            return tokenInfo;
        }).ToList();

        var tokenList = await Task.WhenAll(mapTasks);
        foreach (var tokenInfo in tokenList)
        {
            result[tokenInfo.Symbol] = tokenInfo;
        }
        
        return result;
    }
    
    private bool IsETransfer(string transactionType, string fromChainId, string fromAddress)
    {
        if (transactionType == ActivityConstants.TransferName &&
            _activityOptions.ETransferConfigs != null)
        {
            var eTransferConfig =
                _activityOptions.ETransferConfigs.FirstOrDefault(e => e.ChainId == fromChainId);
            return eTransferConfig != null && eTransferConfig.Accounts.Contains(fromAddress);
        }

        return false;
    }
    
    private void SetDAppInfo(string toContractAddress, GetActivityDto activityDto, string fromAddress,
        string methodName)
    {
        if (activityDto.TransactionType == ActivityConstants.SwapExactTokensForTokensName &&
            _activityOptions.ETransferConfigs.SelectMany(t => t.Accounts).Contains(fromAddress))
        {
            var eTransferConfig = _activityOptions.ETransferConfigs.FirstOrDefault();
            toContractAddress = eTransferConfig?.ContractAddress;
        }

        if (IsETransfer(activityDto.TransactionType, activityDto.FromChainId, activityDto.FromAddress))
        {
            var eTransferConfig = _activityOptions.ETransferConfigs.FirstOrDefault();
            toContractAddress = eTransferConfig?.ContractAddress;
        }

        if (methodName == ActivityConstants.FreeMintNftName)
        {
            activityDto.FromAddress = toContractAddress;
        }

        if (string.IsNullOrEmpty(toContractAddress))
        {
            return;
        }

        var contractConfig =
            _activityOptions.ContractConfigs.FirstOrDefault(t => t.ContractAddress == toContractAddress);

        if (contractConfig != null && !string.IsNullOrEmpty(contractConfig.DappName))
        {
            activityDto.DappName = contractConfig.DappName;
            activityDto.DappIcon = contractConfig.DappIcon;
            return;
        }

        var tokenSpender =
            _tokenSpenderOptions.TokenSpenderList.FirstOrDefault(t => t.ContractAddress == toContractAddress);
        if (tokenSpender == null)
        {
            activityDto.DappName = _activityOptions.UnknownConfig.NotUnknownContracts.Contains(toContractAddress)
                ? string.Empty
                : _activityOptions.UnknownConfig.UnknownName;
            activityDto.DappIcon = activityDto.DappName == _activityOptions.UnknownConfig.UnknownName
                ? _activityOptions.UnknownConfig.UnknownIcon
                : activityDto.DappIcon;
            return;
        }

        activityDto.DappName = tokenSpender.Name;
        activityDto.DappIcon = tokenSpender.Icon;
    }

    private async Task<GetActivityDto> ConvertDtoAsync(string chainId, TransactionDetailDto dto, Dictionary<string, IndexerTokenInfoDto> tokenMap)
    {
        var activityDto = new GetActivityDto
        {
            TransactionId = dto.TransactionId,
            Status = dto.Status.ToString().ToUpper(),
            TransactionName = dto.Method,
            TransactionType = dto.Method,
            ListIcon = null, //todo
            Timestamp = dto.Timestamp.ToString(),
            BlockHash = null, // todo
            FromAddress = dto.From.Address,
            ToAddress = dto.To.Address,
            ChainId = chainId,
            // FromChainId = , //todo
            // FromChainIcon = ,
            // FromChainIdUpdated = ,
            // ToChainId = ,
            // ToChainIcon = ,
            // ToChainIdUpdated = ,
        };
        
        SetDAppInfo(dto.To.Address, activityDto, dto.From.Address, dto.Method);
        
        foreach (var tokenTransferred in dto.TokenTransferreds)
        {
            if (tokenTransferred.From.Address == dto.From.Address)
            {
                var symbolInfo = activityDto.Operations.FirstOrDefault(t => t.Symbol == tokenTransferred.Symbol && t.IsReceived == false);
                if (symbolInfo == null)
                {
                    activityDto.Operations.Add(new OperationItemInfo()
                    {
                        IsReceived = false,
                        Symbol = tokenTransferred.Symbol,
                        Amount = tokenTransferred.Amount.ToString(),
                        Icon = tokenTransferred.ImageUrl,
                        Decimals = tokenMap[tokenTransferred.Symbol].Decimals.ToString()
                    });
                }
                else
                {
                    symbolInfo.Amount = (long.Parse(symbolInfo.Amount) + tokenTransferred.Amount).ToString();
                }
            } 
            else if (tokenTransferred.To.Address == dto.From.Address)
            {
                var symbolInfo = activityDto.Operations.FirstOrDefault(t => t.Symbol == tokenTransferred.Symbol && t.IsReceived);
                if (symbolInfo == null)
                {
                    activityDto.Operations.Add(new OperationItemInfo()
                    {
                        IsReceived = true,
                        Symbol = tokenTransferred.Symbol,
                        Amount = tokenTransferred.Amount.ToString(),
                        Icon = tokenTransferred.ImageUrl,
                        Decimals = tokenMap[tokenTransferred.Symbol].Decimals.ToString()
                    });
                }
                else
                {
                    symbolInfo.Amount = (long.Parse(symbolInfo.Amount) + tokenTransferred.Amount).ToString();
                }
            } 
        }
        
        foreach (var nftsTransferred in dto.NftsTransferreds)
        {
            if (nftsTransferred.From.Address == dto.From.Address)
            {
                var symbolInfo = activityDto.Operations.FirstOrDefault(t => t.Symbol == nftsTransferred.Symbol && t.IsReceived == false);
                if (symbolInfo == null)
                {
                    var isSeed = false;
                    int seedType = 0;
                    if (nftsTransferred.Symbol.StartsWith(TokensConstants.SeedNamePrefix))
                    {
                        isSeed = true;
                        seedType = (int)SeedType.FT;
                    }
                    
                    activityDto.Operations.Add(new OperationItemInfo()
                    {
                        IsReceived = false,
                        Symbol = nftsTransferred.Symbol,
                        Amount = nftsTransferred.Amount.ToString(),
                        NftInfo = new NftDetail()
                        {
                            ImageUrl = nftsTransferred.ImageUrl,
                            Alias = tokenMap[nftsTransferred.Symbol].TokenName,
                            NftId = nftsTransferred.Symbol.Split("-").Last(),
                            IsSeed = isSeed,
                            SeedType = seedType
                        }
                    });
                }
                else
                {
                    symbolInfo.Amount = (long.Parse(symbolInfo.Amount) + nftsTransferred.Amount).ToString();
                }
            } 
            else if (nftsTransferred.To.Address == dto.From.Address)
            {
                var symbolInfo = activityDto.Operations.FirstOrDefault(t => t.Symbol == nftsTransferred.Symbol && t.IsReceived);
                if (symbolInfo == null)
                {
                    var isSeed = false;
                    int seedType = 0;
                    if (nftsTransferred.Symbol.StartsWith(TokensConstants.SeedNamePrefix))
                    {
                        isSeed = true;
                        seedType = (int)SeedType.FT;
                    }

                    activityDto.Operations.Add(new OperationItemInfo()
                    {
                        IsReceived = true,
                        Symbol = nftsTransferred.Symbol,
                        Amount = nftsTransferred.Amount.ToString(),
                        NftInfo = new NftDetail()
                        {
                            ImageUrl = nftsTransferred.ImageUrl,
                            Alias = tokenMap[nftsTransferred.Symbol].TokenName,
                            NftId = nftsTransferred.Symbol.Split("-").Last(),
                            IsSeed = isSeed,
                            SeedType = seedType
                        }
                    });
                }
                else
                {
                    symbolInfo.Amount = (long.Parse(symbolInfo.Amount) + nftsTransferred.Amount).ToString();
                }
            }
        }
        
        return activityDto;
    }
}