using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Provider;
using EoaServer.Provider.Dto.Indexer;
using EoaServer.Token;
using EoaServer.Token.Dto;
using EoaServer.UserActivity.Dto;
using EoaServer.UserActivity.Dtos;
using EoaServer.UserAssets;
using EoaServer.UserAssets.Dtos;
using EoaServer.UserAssets.Provider;
using Google.Protobuf.WellKnownTypes;
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
    private readonly IImageProcessProvider _imageProcessProvider;
    private readonly IGraphQLProvider _graphqlProvider;
    
    public UserActivityAppService(ILogger<UserActivityAppService> logger,
        IOptionsSnapshot<ActivityOptions> activityOptions,
        IOptionsSnapshot<TokenSpenderOptions> tokenSpenderOptions,
        IOptionsSnapshot<ChainOptions> chainOptions,
        ITokenInfoProvider tokenInfoProvider,
        IImageProcessProvider imageProcessProvider,
        IAElfScanDataProvider aelfScanDataProvider,
        IGraphQLProvider graphqlProvider)
    {
        _logger = logger;
        _activityOptions = activityOptions.Value;
        _tokenSpenderOptions = tokenSpenderOptions.Value;
        _chainOptions = chainOptions.Value;
        _tokenInfoProvider = tokenInfoProvider;
        _aelfScanDataProvider = aelfScanDataProvider;
        _imageProcessProvider = imageProcessProvider;
        _graphqlProvider = graphqlProvider;
    }
    
    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request)
    {
        var txnDto = await _aelfScanDataProvider.GetTransactionDetailAsync(request.ChainId, request.TransactionId);

        if (txnDto == null || txnDto.List.Count < 1)
        {
            _logger.LogError($"Get TransactionDetailResponseDto failed, chainId: {request.ChainId}, transactionId: {request.TransactionId}");
            return null;
        }
        var tokens = new HashSet<string>(
            txnDto.List
                .SelectMany(txn => txn.TokenTransferreds
                    .Select(transfer => transfer.Symbol)
                    .Concat(txn.NftsTransferreds
                        .Select(transfer => transfer.Symbol)))
        );
        var tokenMap = await GetTokenMapAsync(tokens);
        return await ConvertDtoAsync(request.ChainId, txnDto.List[0], tokenMap, 0, 0, request.AddressInfos[0].Address);
    }
    
    public async Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request)
    {
        var address = request.AddressInfos[0].Address;
        var chainId = request.AddressInfos.Count == 1 ? request.AddressInfos[0].ChainId : "";

        var txns = new TransactionsResponseDto();
        var tokenTransfers = new IndexerTokenTransferListDto();
        
        var txnsTask = _aelfScanDataProvider.GetAddressTransactionsAsync(chainId, address, 0, request.SkipCount + request.MaxResultCount);
        var tokenTransfersTask = _graphqlProvider.GetTokenTransferInfoAsync(new TokenTransferInput()
        {
            Address = address,
            ChainId = chainId,
            SkipCount = 0,
            MaxResultCount = request.SkipCount + request.MaxResultCount,
            Symbol = request.Symbol
        });

        await Task.WhenAll(txnsTask, tokenTransfersTask);

        txns = await txnsTask;
        tokenTransfers = await tokenTransfersTask;
        
        if (tokenTransfers != null)
        {
            foreach (var transfer in tokenTransfers.Items)
            {
                var txn = txns.Transactions.FirstOrDefault(t => t.TransactionId == transfer.TransactionId);
                if (txn == null)
                {
                    txns.Transactions.Add(new TransactionResponseDto
                    {
                        TransactionId = transfer.TransactionId,
                        ChainIds = new List<string>(){transfer.Metadata.ChainId},
                        Timestamp = DateTimeHelper.ToUnixTimeSeconds(transfer.Metadata.Block.BlockTime)
                    });
                }
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
            return await _aelfScanDataProvider.GetTransactionDetailAsync(txn.ChainIds[0], txn.TransactionId);
        }).ToList();

        var txnDetailMap = (await Task.WhenAll(mapTasks))
            .Where(result => result != null)
            .SelectMany(result => result.List)
            .ToDictionary(t => t.TransactionId, t => t);

        var tokens = new HashSet<string>(
            txnDetailMap
                .SelectMany(txn => txn.Value.TokenTransferreds
                    .Select(transfer => transfer.Symbol)
                    .Concat(txn.Value.NftsTransferreds
                        .Select(transfer => transfer.Symbol)))
        );
        
        var tokenMap = await GetTokenMapAsync(tokens);
        var activityDtos = new List<GetActivityDto>();
        foreach (var txn in txns.Transactions)
        {
            if (txnDetailMap.ContainsKey(txn.TransactionId) && txnDetailMap[txn.TransactionId] != null)
            {
                activityDtos.Add(await ConvertDtoAsync(txn.ChainIds[0], txnDetailMap[txn.TransactionId], tokenMap, request.Width, request.Height, request.AddressInfos[0].Address));
            }
            else
            {
                _logger.LogError($"Get transaction detail error. ChainId: {txn.ChainIds[0]}, TransactionId: {txn.TransactionId}");
            }
        }
        
        return new GetActivitiesDto()
        {
            Data = activityDtos
        };
    }

    private async Task<Dictionary<string, TokenInfoDto>> GetTokenMapAsync(HashSet<string> tokens)
    {
        var result = tokens.ToDictionary(t => t, t => new TokenInfoDto());

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

    private async Task<GetActivityDto> ConvertDtoAsync(string chainId, TransactionDetailDto dto, Dictionary<string, TokenInfoDto> tokenMap, int width, int height, string userAddress)
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
            if (tokenTransferred.To.Address == userAddress || tokenTransferred.From.Address == userAddress)
            {
                var isReceived = tokenTransferred.To.Address == userAddress;
                var symbolInfo = activityDto.Operations.FirstOrDefault(t => t.Symbol == tokenTransferred.Symbol);
                if (symbolInfo == null)
                {
                    activityDto.Operations.Add(new OperationItemInfo()
                    {
                        IsReceived = isReceived,
                        Symbol = tokenTransferred.Symbol,
                        Amount = tokenTransferred.Amount.ToString(),
                        Icon = tokenTransferred.ImageUrl,
                        Decimals = tokenMap[tokenTransferred.Symbol]?.Decimals.ToString()
                    });
                }
                else
                {
                    if (isReceived == symbolInfo.IsReceived)
                    {
                        symbolInfo.Amount = (long.Parse(symbolInfo.Amount) + tokenTransferred.Amount).ToString();
                    }
                    else
                    {
                        symbolInfo.Amount = (long.Parse(symbolInfo.Amount) - tokenTransferred.Amount).ToString();
                    }
                
                }
            }
        }
        
        foreach (var nftsTransferred in dto.NftsTransferreds)
        {
            if (nftsTransferred.To.Address == userAddress || nftsTransferred.From.Address == userAddress)
            {
                var isReceived = nftsTransferred.To.Address == userAddress;
                var symbolInfo = activityDto.Operations.FirstOrDefault(t => t.Symbol == nftsTransferred.Symbol);
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
                        ImageUrl = await _imageProcessProvider.GetResizeImageAsync(
                            nftsTransferred.ImageUrl, width, height,
                            ImageResizeType.Forest),
                        Alias = tokenMap[nftsTransferred.Symbol]?.TokenName,
                        NftId = nftsTransferred.Symbol.Split("-").Last(),
                        IsSeed = isSeed,
                        SeedType = seedType
                    };
                    activityDto.Operations.Add(new OperationItemInfo()
                    {
                        IsReceived = isReceived,
                        Symbol = nftsTransferred.Symbol,
                        Amount = nftsTransferred.Amount.ToString(),
                        NftInfo = nftInfo
                    });
                    activityDto.NftInfo = nftInfo;
                }
                else
                {
                    if (isReceived == symbolInfo.IsReceived)
                    {
                        symbolInfo.Amount = (long.Parse(symbolInfo.Amount) + nftsTransferred.Amount).ToString();
                    }
                    else
                    {
                        symbolInfo.Amount = (long.Parse(symbolInfo.Amount) - nftsTransferred.Amount).ToString();
                    }
                }
            }
        }

        foreach (var operation in activityDto.Operations)
        {
            if (long.Parse(operation.Amount) < 0)
            {
                operation.IsReceived = !operation.IsReceived;
                operation.Amount = (long.Parse(operation.Amount) * -1).ToString();
            }
        }

        if (activityDto.Operations.Count == 1)
        {
            var tokenPrice = 0d;
            if (activityDto.Operations[0].NftInfo == null)
            {
                var token = dto.TokenTransferreds.FirstOrDefault(t => t.Symbol == activityDto.Operations[0].Symbol);
                if (token != null && token.NowPrice != null && token.AmountString != null)
                {
                    tokenPrice = double.Parse(token.NowPrice) / double.Parse(token.AmountString);
                }
            }
            else
            {
                var token = dto.NftsTransferreds.FirstOrDefault(t => t.Symbol == activityDto.Operations[0].Symbol);
                if (token != null && token.NowPrice != null && token.AmountString != null)
                {
                    tokenPrice = double.Parse(token.NowPrice) / double.Parse(token.AmountString);
                }
            }

            var amount = double.Parse(activityDto.Operations[0]?.Amount ?? "0") /
                         Math.Pow(10, double.Parse(activityDto.Operations[0]?.Decimals ?? "0"));
            activityDto.Symbol = activityDto.Operations[0].Symbol;
            activityDto.Amount = activityDto.Operations[0].Amount;
            activityDto.Decimals = activityDto.Operations[0].Decimals;
            activityDto.NftInfo = activityDto.Operations[0].NftInfo;
            activityDto.CurrentPriceInUsd = tokenPrice.ToString();
            activityDto.CurrentTxPriceInUsd = (tokenPrice * amount).ToString();
            activityDto.IsReceived = activityDto.Operations[0].IsReceived;
            activityDto.Operations.Clear();
        }
        
        activityDto.ListIcon = activityDto.Operations.FirstOrDefault()?.Icon;
        
        return activityDto;
    }
}