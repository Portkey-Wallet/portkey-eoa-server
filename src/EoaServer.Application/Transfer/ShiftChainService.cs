using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Options;
using EoaServer.Token;
using EoaServer.Transfer.Dtos;
using EoaServer.Transfer.Provider;
using EoaServer.Transfer.Proxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using AddressHelper = EoaServer.Commons.AddressHelper;

namespace EoaServer.Transfer;

[RemoteService(false), DisableAuditing]
public class ShiftChainService : EoaServerBaseService, IShiftChainService
{
    private readonly IETransferProxyService _eTransferProxyService;
    private readonly ChainOptions _chainOptions;
    private readonly ITokenAppService _tokenAppService;
    private readonly IHttpClientService _httpClientService;
    private readonly ETransferOptions _eTransferOptions;
    private readonly INetworkCacheProvider _networkCacheProvider;
    private readonly ITransferAppService _transferAppService;
    private readonly ILogger<ShiftChainService> _logger;

    public ShiftChainService(IETransferProxyService eTransferProxyService,
        IOptionsSnapshot<ChainOptions> chainOptions, ITokenAppService tokenAppService,
        IHttpClientService httpClientService,
        IOptionsSnapshot<ETransferOptions> eTransferOptions, INetworkCacheProvider networkCacheProvider,
        ITransferAppService transferAppService,
        ILogger<ShiftChainService> logger)
    {
        _eTransferProxyService = eTransferProxyService;
        _chainOptions = chainOptions.Value;
        _tokenAppService = tokenAppService;
        _httpClientService = httpClientService;
        _eTransferOptions = eTransferOptions.Value;
        _networkCacheProvider = networkCacheProvider;
        _transferAppService = transferAppService;
        _logger = logger;
    }

    public async Task InitAsync()
    {
        var receiveNetworkMap = new Dictionary<string, ReceiveNetworkDto>();
        var networkMap = new Dictionary<string, NetworkInfoDto>();
        var sendEBridgeMap = new Dictionary<string, SendNetworkDto>();

        await SetReceiveByETransfer(receiveNetworkMap, networkMap);

        var limiter = await SetReceiveByEBridge(receiveNetworkMap, networkMap);
        SetSendByEBridge(sendEBridgeMap, networkMap, limiter);

        foreach (var chainName in _chainOptions.ChainInfos.Keys)
        {
            networkMap[chainName] = ShiftChainHelper.GetAELFInfo(chainName);
        }

        _networkCacheProvider.SetCache(receiveNetworkMap, networkMap, sendEBridgeMap);
    }


    private readonly SemaphoreSlim _semaphoreReceive = new SemaphoreSlim(1, 1);

    public async Task<ResponseWrapDto<ReceiveNetworkDto>> GetReceiveNetworkList(GetReceiveNetworkListRequestDto request)
    {
        var result = _networkCacheProvider.GetReceiveNetworkList(request);
        if (null == result)
        {
            await _semaphoreReceive.WaitAsync();
            try
            {
                result = _networkCacheProvider.GetReceiveNetworkList(request);
                if (null == result)
                {
                    await InitAsync();
                    result = _networkCacheProvider.GetReceiveNetworkList(request);
                }
            }
            finally
            {
                _semaphoreReceive.Release();
            }
        }

        return new ResponseWrapDto<ReceiveNetworkDto>
        {
            Code = ETransferConstant.SuccessCode,
            Data = result
        };
    }

    public async Task<ResponseWrapDto<SendNetworkDto>> GetSendNetworkList(GetSendNetworkListRequestDto request)
    {
        var result = new SendNetworkDto { NetworkList = new List<NetworkInfoDto>() };
        var addressFormat = ShiftChainHelper.GetAddressFormat(request.ChainId, request.ToAddress);
        if (addressFormat == AddressFormat.NoSupport)
        {
            return new ResponseWrapDto<SendNetworkDto>()
            {
                Code = ETransferConstant.InvalidAddressCode,
                Message = ETransferConstant.InvalidAddressMessage
            };
        }

        if (request.ToAddress.Contains(CommonConstant.Underline) && request.ToAddress.StartsWith(CommonConstant.ELF))
        {
            return new ResponseWrapDto<SendNetworkDto>
            {
                Code = ETransferConstant.SuccessCode,
                Data = result
            };
        }

        if (addressFormat is AddressFormat.Main or AddressFormat.Dapp)
        {
            result.NetworkList.Add(_networkCacheProvider.GetNetwork(request.ChainId));
        }

        await SetSendByETransfer(result, request);

        await SetSendByEBridgeAsync(result, request);

        if (result.NetworkList.Count == 0)
        {
            return new ResponseWrapDto<SendNetworkDto>
            {
                Code = ETransferConstant.InvalidAddressCode,
                Message = ETransferConstant.InvalidAddressMessage
            };
        }

        return new ResponseWrapDto<SendNetworkDto>
        {
            Code = ETransferConstant.SuccessCode,
            Data = result
        };
    }

    private async Task SetSendByEBridgeAsync(SendNetworkDto result, GetSendNetworkListRequestDto request)
    {
        // set ebridge
        var ebridge = _networkCacheProvider.GetSendNetworkList(request);
        if (ebridge?.Count > 0)
        {
            foreach (var network in ebridge)
            {
                var service = new ServiceDto
                {
                    ServiceName = ShiftChainHelper.EBridgeTool,
                    MultiConfirmTime = ShiftChainHelper.GetTime(request.ChainId, network.Network),
                };
                var orgNet = result.NetworkList.FirstOrDefault(p => p.Network == network.Network);
                if (null == orgNet)
                {
                    orgNet = new NetworkInfoDto
                    {
                        Name = network.Name,
                        Network = network.Network,
                        ImageUrl = ShiftChainHelper.GetChainImage(network.Network),
                        ServiceList = new List<ServiceDto> { service }
                    };
                    result.NetworkList.Add(orgNet);
                }
                else
                {
                    orgNet.ServiceList.Add(service);
                }
            }
        }
    }

    private async Task SetSendByETransfer(SendNetworkDto result, GetSendNetworkListRequestDto request)
    {
        // set etransfer
        string formatAddress = ShiftChainHelper.ExtractAddress(request.ToAddress);
        ResponseWrapDto<GetNetworkListDto> etransfer = null;
        try
        {
            etransfer = await _eTransferProxyService.GetNetworkListAsync(new GetNetworkListRequestDto
            {
                Type = "Withdraw", Symbol = request.Symbol, ChainId = request.ChainId, Address = formatAddress
            });
        }
        catch (Exception e)
        {
            return;
        }

        if (etransfer?.Data?.NetworkList?.Count != 0)
        {
            var price = await _tokenAppService.GetTokenPriceListAsync(new List<string> { request.Symbol });
            var maxAmount = ShiftChainHelper.GetMaxAmount(price.Items[0].PriceInUsd);
            foreach (var networkDto in etransfer.Data.NetworkList)
            {
                result.NetworkList.Add(new NetworkInfoDto
                {
                    Name = networkDto.Name,
                    Network = networkDto.Network,
                    ImageUrl = ShiftChainHelper.GetChainImage(networkDto.Network),
                    ServiceList = new List<ServiceDto>
                    {
                        new ServiceDto
                        {
                            ServiceName = ShiftChainHelper.ETransferTool,
                            MultiConfirmTime = networkDto.MultiConfirmTime,
                            MaxAmount = maxAmount
                        }
                    }
                });
            }
        }
    }


    private async Task SetReceiveByETransfer(Dictionary<string, ReceiveNetworkDto> receiveNetworkMap,
        Dictionary<string, NetworkInfoDto> networkMap)
    {
        string type = "Deposit";
        var optionList =
            await _transferAppService.GetTokenOptionListAsync(new GetTokenOptionListRequestDto { Type = type });
        foreach (var token in optionList.Data.TokenList)
        {
            var toToken = token.ToTokenList.FirstOrDefault(p => p.Symbol.Equals(token.Symbol));
            if (toToken == null)
            {
                continue;
            }

            string symbol = token.Symbol;
            ReceiveNetworkDto receiveNetwork = InitAelfChain(symbol);
            receiveNetworkMap[symbol] = receiveNetwork;
            var price = await _tokenAppService.GetTokenPriceListAsync(new List<string> { symbol });
            _logger.LogInformation("setReceiveByETransfer symbol = {0} price = {1}", symbol,
                JsonConvert.SerializeObject(price));
            var maxAmount = ShiftChainHelper.GetMaxAmount(price.Items[0].PriceInUsd);
            foreach (var chainId in toToken.ChainIdList)
            {
                var networkList = await _eTransferProxyService.GetNetworkListAsync(new GetNetworkListRequestDto
                {
                    Type = type, Symbol = token.Symbol, ChainId = chainId,
                });
                foreach (var networkDto in networkList.Data.NetworkList)
                {
                    receiveNetwork.DestinationMap[chainId].Add(new NetworkInfoDto
                    {
                        Network = networkDto.Network,
                        Name = networkDto.Name,
                        ImageUrl = ShiftChainHelper.GetChainImage(networkDto.Network),
                        ServiceList = new List<ServiceDto>
                        {
                            new ServiceDto
                            {
                                ServiceName = ShiftChainHelper.ETransferTool,
                                MultiConfirmTime = networkDto.MultiConfirmTime,
                                MaxAmount = maxAmount
                            }
                        }
                    });
                    networkMap[networkDto.Network] = new NetworkInfoDto
                    {
                        Network = networkDto.Network,
                        Name = networkDto.Name,
                        ImageUrl = ShiftChainHelper.GetChainImage(networkDto.Network),
                    };
                }
            }
        }
    }

    private async Task<EBridgeLimiterDto> SetReceiveByEBridge(Dictionary<string, ReceiveNetworkDto> receiveNetworkMap,
        Dictionary<string, NetworkInfoDto> networkMap)
    {
        var limiters = await _httpClientService.GetAsync<EBridgeLimiterDto>(_eTransferOptions.EBridgeLimiterUrl);
        foreach (var limiter in limiters.Items)
        {
            foreach (var tokenInfo in limiter.ReceiptRateLimitsInfo)
            {
                if (tokenInfo.Token.Equals("AGENT"))
                {
                    continue;
                }

                if (!receiveNetworkMap.TryGetValue(tokenInfo.Token, out var network))
                {
                    network = InitAelfChain(tokenInfo.Token);
                    receiveNetworkMap[tokenInfo.Token] = network;
                }

                if (!network.DestinationMap.ContainsKey(limiter.ToChain))
                {
                    continue;
                }

                NetworkInfoDto networkInfo = ShiftChainHelper.GetNetworkInfoByEBridge(networkMap, limiter.FromChain);
                var destinationNetworks = network.DestinationMap[limiter.ToChain];
                var destinationNetwork = destinationNetworks.FirstOrDefault(p => p.Network.Equals(networkInfo.Network));

                var serviceDto = new ServiceDto
                {
                    ServiceName = ShiftChainHelper.EBridgeTool,
                    MultiConfirmTime = ShiftChainHelper.GetTime(limiter.FromChain, limiter.ToChain)
                };
                if (destinationNetwork == null)
                {
                    destinationNetwork = new NetworkInfoDto
                    {
                        Network = networkInfo.Network,
                        Name = networkInfo.Name,
                        ImageUrl = networkInfo.ImageUrl,
                        ServiceList = new List<ServiceDto> { serviceDto }
                    };
                    destinationNetworks.Add(destinationNetwork);
                }
                else
                {
                    destinationNetwork.ServiceList.Add(serviceDto);
                }
            }
        }

        return limiters;
    }

    private static void SetSendByEBridge(Dictionary<string, SendNetworkDto> sendEBridgeMap,
        Dictionary<string, NetworkInfoDto> networkMap,
        EBridgeLimiterDto limiters)
    {
        foreach (var limiter in limiters.Items)
        {
            foreach (var tokenInfo in limiter.ReceiptRateLimitsInfo)
            {
                if (tokenInfo.Token.Equals("AGENT"))
                {
                    continue;
                }

                if (!CommonConstant.ChainIds.Contains(limiter.ToChain))
                {
                    continue;
                }

                string key = tokenInfo.Token + ";" + ShiftChainHelper.FormatEBridgeChain(limiter.ToChain);
                if (!sendEBridgeMap.TryGetValue(key, out var sendInfo))
                {
                    sendInfo = new SendNetworkDto { NetworkList = new List<NetworkInfoDto>() };
                    sendEBridgeMap[key] = sendInfo;
                }

                if (!sendInfo.NetworkList.Any(p => p.Network == limiter.FromChain))
                {
                    sendInfo.NetworkList.Add(ShiftChainHelper.GetNetworkInfoByEBridge(networkMap, limiter.FromChain));
                }
            }
        }
    }

    private ReceiveNetworkDto InitAelfChain(string symbol)
    {
        var receiveNetwork = new ReceiveNetworkDto();
        var chainIds = _chainOptions.ChainInfos.Keys;
        foreach (var chainId in chainIds)
        {
            receiveNetwork.DestinationMap[chainId] = new List<NetworkInfoDto>();
            foreach (var chainInfosKey in chainIds)
            {
                receiveNetwork.DestinationMap[chainId].Add(ShiftChainHelper.GetAELFInfo(chainInfosKey));
            }
        }

        return receiveNetwork;
    }

    public Task<GetSupportNetworkDto> GetSupportNetworkListAsync()
    {
        var supportedNetworks = new Dictionary<string, Dictionary<string, List<NetworkBasicInfo>>>();

        _chainOptions.ChainInfos.Keys.ToList().ForEach(chainId =>
        {
            supportedNetworks[chainId] = new Dictionary<string, List<NetworkBasicInfo>>();
        });

        var networkMap = _networkCacheProvider.GetReceiveNetworkMap();
        foreach (var networkItem in networkMap)
        {
            var symbol = networkItem.Key;
            var destinationMap = networkItem.Value.DestinationMap;
            foreach (var destNetworkItem in destinationMap)
            {
                var chainId = destNetworkItem.Key;
                var supportedNetwork = supportedNetworks[chainId];
                if (!supportedNetwork.ContainsKey(symbol))
                {
                    supportedNetwork[symbol] = new List<NetworkBasicInfo>();
                }

                var supportedNetworkList = supportedNetwork[symbol];
                foreach (var item in destNetworkItem.Value)
                {
                    if (supportedNetworkList.FirstOrDefault(t =>
                            string.Equals(t.Network, item.Network, StringComparison.CurrentCultureIgnoreCase)) != null)
                        continue;
                    supportedNetworkList.Add(new NetworkBasicInfo()
                    {
                        Network = item.Network == CommonConstant.BaseNetwork
                            ? CommonConstant.BaseNetworkName
                            : item.Network,
                        Name = AddressHelper.GetNetworkName(item.Network)
                    });
                }
            }
        }

        return Task.FromResult(new GetSupportNetworkDto()
        {
            SupportedNetworks = supportedNetworks
        });
    }
}