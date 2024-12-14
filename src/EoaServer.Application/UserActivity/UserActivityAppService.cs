using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token;
using EoaServer.Token.Dto;
using EoaServer.UserActivity.Dto;
using EoaServer.UserActivity.Dtos;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Volo.Abp;
using Volo.Abp.Auditing;
using Newtonsoft.Json;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using TokenInfoDto = EoaServer.Token.Dto.TokenInfoDto;

namespace EoaServer.UserActivity;

[RemoteService(false)]
[DisableAuditing]
public class UserActivityAppService : EoaServerBaseService, IUserActivityAppService
{
    private readonly ILogger<UserActivityAppService> _logger;
    private readonly ActivityOptions _activityOptions;
    private readonly TokenSpenderOptions _tokenSpenderOptions;
    private readonly ChainOptions _chainOptions;
    private readonly ITokenInfoProvider _tokenInfoProvider;
    private readonly IAElfScanDataProvider _aelfScanDataProvider;

    public UserActivityAppService(ILogger<UserActivityAppService> logger,
        IOptionsSnapshot<ActivityOptions> activityOptions,
        IOptionsSnapshot<TokenSpenderOptions> tokenSpenderOptions,
        IOptionsSnapshot<ChainOptions> chainOptions,
        ITokenInfoProvider tokenInfoProvider,
        IAElfScanDataProvider aelfScanDataProvider)
    {
        _logger = logger;
        _activityOptions = activityOptions.Value;
        _tokenSpenderOptions = tokenSpenderOptions.Value;
        _chainOptions = chainOptions.Value;
        _tokenInfoProvider = tokenInfoProvider;
        _aelfScanDataProvider = aelfScanDataProvider;

    }
    
    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request)
    {
        var txnDto = await _aelfScanDataProvider.GetTransactionDetailAsync(request.ChainId, request.TransactionId);

        if (txnDto == null || txnDto.List.Count < 1)
        {
            _logger.LogError($"Get TransactionDetailResponseDto failed, chainId: {request.ChainId}, transactionId: {request.TransactionId}");
            return null;
        }

        var tokenMap = await GetTokenMapAsync(txnDto.List);
        return await ConvertDtoAsync(request.ChainId, txnDto.List[0], tokenMap);
    }
    
    public async Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request)
    {
        var address = request.AddressInfos[0].Address;
        var chainId = request.AddressInfos.Count == 1 ? request.AddressInfos[0].ChainId : null;
        
        var txnsTask = _aelfScanDataProvider.GetAddressTransactionsAsync(chainId, address, 0, request.SkipCount + request.MaxResultCount);
        var tokenTransfersTask = _aelfScanDataProvider.GetAddressTransfersAsync(chainId, address, 0, 0, request.SkipCount + request.MaxResultCount);
        var nftTransfersTask = _aelfScanDataProvider.GetAddressTransfersAsync(chainId, address, 1, 0, request.SkipCount + request.MaxResultCount);

        await Task.WhenAll(txnsTask, tokenTransfersTask, nftTransfersTask);

        var txns = await txnsTask;
        var tokenTransfers = await tokenTransfersTask;
        var nftTransfers = await nftTransfersTask;

        tokenTransfers.List.AddRange(nftTransfers.List);
        
        foreach (var transfer in tokenTransfers.List)
        {
            var txn = txns.Transactions.FirstOrDefault(t => t.TransactionId == transfer.TransactionId);
            if (txn == null)
            {
                txns.Transactions.Add(new TransactionResponseDto
                {
                    TransactionId = transfer.TransactionId,
                    ChainIds = transfer.ChainIds,
                    Timestamp = transfer.BlockTime
                });
            }
        }
        
        txns.Transactions = txns.Transactions.OrderByDescending(item => item.Timestamp)
            .Skip(request.SkipCount) 
            .Take(request.MaxResultCount)
            .ToList();
        
        var txnChainMap = new Dictionary<string, string>();
        var mapTasks = txns.Transactions.Select(async txn =>
        {
            if (txn.ChainIds.Count < 1)
            {
                return null;
            }
            txnChainMap[txn.TransactionId] = txn.ChainIds[0];
            var txnDetail = await _aelfScanDataProvider.GetTransactionDetailAsync(txn.ChainIds[0], txn.TransactionId);
            return txnDetail;
        }).ToList();

        var txnDetails = (await Task.WhenAll(mapTasks))
            .Where(result => result != null)
            .SelectMany(result => result.List) 
            .ToList();
        
        var tokenMap = await GetTokenMapAsync(txnDetails);
        var activityDtos = new List<GetActivityDto>();
        foreach (var txnDetail in txnDetails)
        {
            activityDtos.Add(await ConvertDtoAsync(txnChainMap[txnDetail.TransactionId], txnDetail, tokenMap));
        }
        
        return new GetActivitiesDto()
        {
            Data = activityDtos
        };
    }

    private async Task<Dictionary<string, TokenInfoDto>> GetTokenMapAsync(List<TransactionDetailDto> txnDetails)
    {
        var result = new Dictionary<string, TokenInfoDto>();
        foreach (var transactionDetail in txnDetails)
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

        var sideChain = _chainOptions.ChainInfos.FirstOrDefault(t => t.Value.IsMainChain == false);
        var tokenChain = sideChain.Value.ChainId;
        
        var mapTasks = result.Select(async token =>
        {
            return await _tokenInfoProvider.GetAsync(tokenChain, token.Key);
        }).ToList();

        var tokenList = await Task.WhenAll(mapTasks);
        foreach (var tokenInfo in tokenList)
        {
            if (tokenInfo != null)
            {
                result[tokenInfo.Symbol] = tokenInfo;
            }
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

    private async Task<GetActivityDto> ConvertDtoAsync(string chainId, TransactionDetailDto dto, Dictionary<string, TokenInfoDto> tokenMap)
    {
        var activityDto = new GetActivityDto
        {
            TransactionId = dto.TransactionId,
            Status = dto.Status.ToString().ToUpper(),
            TransactionName = dto.Method,
            TransactionType = dto.Method,
            Timestamp = dto.Timestamp.ToString(),
            FromAddress = dto.From.Address,
            ToAddress = dto.To.Address,
            ChainId = chainId,
            FromChainId = chainId, 
            FromChainIcon = ChainDisplayNameHelper.MustGetChainUrl(chainId),
            FromChainIdUpdated = ChainDisplayNameHelper.MustGetChainDisplayName(chainId),
            ToChainId = chainId,
            ToChainIcon = ChainDisplayNameHelper.MustGetChainUrl(chainId),
            ToChainIdUpdated = ChainDisplayNameHelper.MustGetChainDisplayName(chainId),
        };

        if (dto.TransactionFees != null)
        {
            foreach (var dtoTransactionFee in dto.TransactionFees)
            {
                activityDto.TransactionFees.Add(new TransactionFee
                {
                    Symbol = dtoTransactionFee.Symbol,
                    Fee = dtoTransactionFee.Amount,
                    FeeInUsd = dtoTransactionFee.NowPrice,
                    Decimals = tokenMap[dtoTransactionFee.Symbol]?.Decimals.ToString()
                });
            }
        }
        
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
                        Decimals = tokenMap[tokenTransferred.Symbol]?.Decimals.ToString()
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
                        Decimals = tokenMap[tokenTransferred.Symbol]?.Decimals.ToString()
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

                    var nftInfo = new NftDetail()
                    {
                        ImageUrl = nftsTransferred.ImageUrl,
                        Alias = tokenMap[nftsTransferred.Symbol]?.TokenName,
                        NftId = nftsTransferred.Symbol.Split("-").Last(),
                        IsSeed = isSeed,
                        SeedType = seedType
                    };
                    activityDto.Operations.Add(new OperationItemInfo()
                    {
                        IsReceived = false,
                        Symbol = nftsTransferred.Symbol,
                        Amount = nftsTransferred.Amount.ToString(),
                        NftInfo = nftInfo
                    });
                    activityDto.NftInfo = nftInfo;
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

                    var nftInfo = new NftDetail()
                    {
                        ImageUrl = nftsTransferred.ImageUrl,
                        Alias = tokenMap[nftsTransferred.Symbol]?.TokenName,
                        NftId = nftsTransferred.Symbol.Split("-").Last(),
                        IsSeed = isSeed,
                        SeedType = seedType
                    };
                    activityDto.Operations.Add(new OperationItemInfo()
                    {
                        IsReceived = true,
                        Symbol = nftsTransferred.Symbol,
                        Amount = nftsTransferred.Amount.ToString(),
                        NftInfo = nftInfo
                    });
                    activityDto.NftInfo = nftInfo;
                }
                else
                {
                    symbolInfo.Amount = (long.Parse(symbolInfo.Amount) + nftsTransferred.Amount).ToString();
                }
            }
        }

        activityDto.ListIcon = activityDto.Operations.FirstOrDefault()?.Icon;
        
        _logger.LogInformation($"convert transaction: {dto.TransactionId}, from: {JsonConvert.SerializeObject(dto)}, to: {JsonConvert.SerializeObject(activityDto)}");
        
        return activityDto;
    }
}