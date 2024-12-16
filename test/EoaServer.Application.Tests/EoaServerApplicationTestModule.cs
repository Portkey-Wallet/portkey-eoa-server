using System.Collections.Generic;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.EntityEventHandler.Core;
using EoaServer.Grain.Tests;
using EoaServer.Options;
using EoaServer.Token;
using EoaServer.Token.Dto;
using EoaServer.UserActivity;
using EoaServer.UserActivity.Dto;
using EoaServer.UserAssets.Dtos;
using EoaServer.UserToken;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using TokenInfoDto = EoaServer.UserAssets.Dtos.TokenInfoDto;

namespace EoaServer;

[DependsOn(
    typeof(EoaServerApplicationModule),
    typeof(AbpEventBusModule),
    typeof(EoaServerGrainTestModule),
    typeof(EoaServerDomainTestModule),
    typeof(EoaServerEntityEventHandlerCoreModule)
)]
public class EoaServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<EoaServerApplicationModule>(); });
        context.Services.AddSingleton<IUserActivityAppService, UserActivityAppService>();
        context.Services.AddSingleton<IUserTokenAppService, UserTokenAppService>();
        context.Services.AddSingleton<ITokenAppService, TokenAppService>();
        
        base.ConfigureServices(context);
        
        MockData(context);
        
        Configure<ActivityOptions>(options =>
        {
            options.ContractConfigs = new List<ContractConfig>()
            {
                new ContractConfig()
                {
                    ContractAddress = EoaServerApplicationTestConstant.ProxyAccountContractAddress,
                    DappName = "Proxy Account Contract"
                }
            };
            options.UnknownConfig = new UnknownConfig()
            {
                NotUnknownContracts = new List<string>()
            };
        });
        
        Configure<TokenSpenderOptions>(options =>
        {
            options.TokenSpenderList = new List<TokenSpender>()
            {
                new TokenSpender()
                {
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW,
                    ContractAddress = EoaServerApplicationTestConstant.ForestContractAddress,
                    Name = "Forest",
                    Icon = "ForestIcon",
                    Url = "ForestUrl"
                }
            };
        });
        
        Configure<ChainOptions>(options =>
        {
            options.ChainInfos = new Dictionary<string, ChainInfo>()
            {
                {
                    EoaServerApplicationTestConstant.ChainIdAELF, new ChainInfo()
                    {
                        ChainId = EoaServerApplicationTestConstant.ChainIdAELF,
                        IsMainChain = true,
                        TokenContractAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE"
                    }
                },
                {
                    EoaServerApplicationTestConstant.ChainIdTDVW, new ChainInfo()
                    {
                        ChainId = EoaServerApplicationTestConstant.ChainIdTDVW,
                        IsMainChain = false,
                        TokenContractAddress = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx"
                    }
                },
            };
        });

        Configure<AElfScanOptions>(options =>
        {
            options.BaseUrl = "mockAElfScanUrl";
        });
        
        Configure<SeedImageOptions>(options =>
        {
            options.SeedImageDic = new Dictionary<string, string>();
        });
        
        Configure<NftItemDisplayOption>(options =>
        {
            options.RecommendedRefreshSeconds = 30;
        });
        
        var tokenList = new List<UserTokenItem>();
        tokenList.Add(new UserTokenItem
        {
            IsDefault = true,
            IsDisplay = true,
            SortWeight = 1,
            Token = new Options.Token
            {
                ChainId = EoaServerApplicationTestConstant.ChainIdAELF,
                Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                Decimals = 8,
                Symbol = "ELF"
            }
        });
        tokenList.Add(new UserTokenItem
        {
            IsDefault = false,
            IsDisplay = false,
            SortWeight = 1,
            Token = new Options.Token
            {
                ChainId = EoaServerApplicationTestConstant.ChainIdTDVW,
                Address = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
                Decimals = 8,
                Symbol = "ELF"
            }
        });
        tokenList.Add(new UserTokenItem
        {
            IsDefault = true,
            IsDisplay = true,
            SortWeight = 1,
            Token = new Options.Token
            {
                ChainId = EoaServerApplicationTestConstant.ChainIdAELF,
                Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                Decimals = 8,
                Symbol = "ETH"
            }
        });
        tokenList.Add(new UserTokenItem
        {
            IsDefault = false,
            IsDisplay = false,
            SortWeight = 1,
            Token = new Options.Token
            {
                ChainId = EoaServerApplicationTestConstant.ChainIdTDVW,
                Address = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
                Decimals = 8,
                Symbol = "ETH"
            }
        });
        
        Configure<TokenListOptions>(options =>
        {
            options.UserToken = tokenList;
        });
    }
    
    private void MockData(ServiceConfigurationContext context)
    {
        
        var mockHttpProvider = new Mock<IHttpClientProvider>();
        MockTokenInfoData(mockHttpProvider);
        MockTransactionData(mockHttpProvider);
        MockAssetsData(mockHttpProvider);
        MockNftAssetsData(mockHttpProvider);
        
        context.Services.AddSingleton<IHttpClientProvider>(mockHttpProvider.Object);
    }

    private void MockNftAssetsData(Mock<IHttpClientProvider> mockHttpProvider)
    {
        mockHttpProvider.Setup(provider => provider.GetDataAsync<GetAddressNftListResultDto>(
            It.Is<string>(url =>
                url.StartsWith("mockAElfScanUrl/" + CommonConstant.AelfScanUserNFTAssetsApi) &&
                url.Contains(
                    $"address={EoaServerApplicationTestConstant.User1Address}&chainId={EoaServerApplicationTestConstant.ChainIdTDVW}&skipCount=0"))
        )).ReturnsAsync(new GetAddressNftListResultDto
        {
            List = new List<AddressNftInfoDto>()
            {
                new AddressNftInfoDto
                {
                    Token = new TokenBaseInfo()
                    {
                        Name = EoaServerApplicationTestConstant.NftBBB2TokenName,
                        Symbol = EoaServerApplicationTestConstant.NftBBB2Symbol,
                        ImageUrl = EoaServerApplicationTestConstant.NftBBB2Icon,
                        Decimals = 0
                    },
                    NftCollection = new TokenBaseInfo()
                    {
                        Name = "xxx",
                        Symbol = EoaServerApplicationTestConstant.NftBBBCollectionSymbol,
                        ImageUrl = "BBB-0_ImageUrl",
                        Decimals = 0
                    },
                    Quantity = 1,
                    ChainIds = new List<string>()
                    {
                        EoaServerApplicationTestConstant.ChainIdTDVW
                    }
                },
                new AddressNftInfoDto
                {
                    Token = new TokenBaseInfo()
                    {
                        Name = EoaServerApplicationTestConstant.NftBBB1TokenName,
                        Symbol = EoaServerApplicationTestConstant.NftBBB1Symbol,
                        ImageUrl = EoaServerApplicationTestConstant.NftBBB1Icon,
                        Decimals = 0
                    },
                    NftCollection = new TokenBaseInfo()
                    {
                        Name = EoaServerApplicationTestConstant.NftBBBCollectionTokenName,
                        Symbol = EoaServerApplicationTestConstant.NftBBBCollectionSymbol,
                        ImageUrl = EoaServerApplicationTestConstant.NftBBBCollectionIcon,
                        Decimals = 0
                    },
                    Quantity = 1,
                    ChainIds = new List<string>()
                    {
                        EoaServerApplicationTestConstant.ChainIdTDVW
                    }
                }
            }
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<GetAddressTokenListResultDto>(
            It.Is<string>(url =>
                url.StartsWith("mockAElfScanUrl/" + CommonConstant.AelfScanUserTokenAssetsApi) &&
                url.Contains(
                    $"address={EoaServerApplicationTestConstant.User1Address}&chainId={EoaServerApplicationTestConstant.ChainIdAELF}&skipCount=0"))
        )).ReturnsAsync(new GetAddressTokenListResultDto
        {
            List = new List<TokenInfoDto>()
            {
                new TokenInfoDto
                {
                    Token = new TokenBaseInfo()
                    {
                        Symbol = EoaServerApplicationTestConstant.TokenElfSymbol,
                        ImageUrl = EoaServerApplicationTestConstant.TokenElfIcon,
                        Decimals = 8
                    },
                    Quantity = 2,
                    ValueOfUsd = 2,
                    PriceOfUsd = 2,
                    PriceOfUsdPercentChange24h = 2,
                    PriceOfElf = 2,
                    ValueOfElf = 2,
                    ChainIds = new List<string>()
                    {
                        EoaServerApplicationTestConstant.ChainIdAELF
                    },
                    Type = SymbolType.Token
                }
            }
        });
    }
    
    private void MockAssetsData(Mock<IHttpClientProvider> mockHttpProvider)
    {
        mockHttpProvider.Setup(provider => provider.GetDataAsync<GetAddressTokenListResultDto>(
            It.Is<string>(url =>
                url.StartsWith("mockAElfScanUrl/" + CommonConstant.AelfScanUserTokenAssetsApi) &&
                url.Contains(
                    $"address={EoaServerApplicationTestConstant.User1Address}&chainId={EoaServerApplicationTestConstant.ChainIdTDVW}&skipCount=0"))
        )).ReturnsAsync(new GetAddressTokenListResultDto
        {
            List = new List<TokenInfoDto>()
            {
                new TokenInfoDto
                {
                    Token = new TokenBaseInfo()
                    {
                        Symbol = EoaServerApplicationTestConstant.TokenElfSymbol,
                        ImageUrl = EoaServerApplicationTestConstant.TokenElfIcon,
                        Decimals = 8
                    },
                    Quantity = 1,
                    ValueOfUsd = 1,
                    PriceOfUsd = 1,
                    PriceOfUsdPercentChange24h = 1,
                    PriceOfElf = 1,
                    ValueOfElf = 1,
                    ChainIds = new List<string>()
                    {
                        EoaServerApplicationTestConstant.ChainIdTDVW
                    },
                    Type = SymbolType.Token
                },
                new TokenInfoDto
                {
                    Token = new TokenBaseInfo()
                    {
                        Symbol = EoaServerApplicationTestConstant.TokenSgrSymbol,
                        ImageUrl = EoaServerApplicationTestConstant.TokenSgrIcon,
                        Decimals = 8
                    },
                    Quantity = 1,
                    ValueOfUsd = 1,
                    PriceOfUsd = 1,
                    PriceOfUsdPercentChange24h = 1,
                    PriceOfElf = 1,
                    ValueOfElf = 1,
                    ChainIds = new List<string>()
                    {
                        EoaServerApplicationTestConstant.ChainIdTDVW
                    },
                    Type = SymbolType.Token
                }
            }
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<GetAddressTokenListResultDto>(
            It.Is<string>(url =>
                url.StartsWith("mockAElfScanUrl/" + CommonConstant.AelfScanUserTokenAssetsApi) &&
                url.Contains(
                    $"address={EoaServerApplicationTestConstant.User1Address}&chainId={EoaServerApplicationTestConstant.ChainIdAELF}&skipCount=0"))
        )).ReturnsAsync(new GetAddressTokenListResultDto
        {
            List = new List<TokenInfoDto>()
            {
                new TokenInfoDto
                {
                    Token = new TokenBaseInfo()
                    {
                        Symbol = EoaServerApplicationTestConstant.TokenElfSymbol,
                        ImageUrl = EoaServerApplicationTestConstant.TokenElfIcon,
                        Decimals = 8
                    },
                    Quantity = 2,
                    ValueOfUsd = 2,
                    PriceOfUsd = 2,
                    PriceOfUsdPercentChange24h = 2,
                    PriceOfElf = 2,
                    ValueOfElf = 2,
                    ChainIds = new List<string>()
                    {
                        EoaServerApplicationTestConstant.ChainIdAELF
                    },
                    Type = SymbolType.Token
                }
            }
        });
    }

    private void MockTransactionData(Mock<IHttpClientProvider> mockHttpProvider)
    {
        mockHttpProvider.Setup(provider => provider.GetDataAsync<TransactionsResponseDto>(
            It.Is<string>(url => 
                url.StartsWith("mockAElfScanUrl/" + CommonConstant.AelfScanUserTransactionsApi) &&
                url.Contains($"chainId={EoaServerApplicationTestConstant.ChainIdTDVW}&address={EoaServerApplicationTestConstant.User1Address}&skipCount=0"))
        )).ReturnsAsync(new TransactionsResponseDto
        {
            Transactions = new List<TransactionResponseDto>()
            {
                new TransactionResponseDto()
                {
                    TransactionId = "0x1",
                    ChainIds = new List<string>(){EoaServerApplicationTestConstant.ChainIdTDVW},
                    Timestamp = 1
                }
            }
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<GetTransferListResultDto>(
            It.Is<string>(url => 
                url.StartsWith("mockAElfScanUrl/" + CommonConstant.AelfScanUserTransfersApi) &&
                url.Contains($"chainId={EoaServerApplicationTestConstant.ChainIdTDVW}&tokenType=0&address={EoaServerApplicationTestConstant.User1Address}&skipCount=0"))
        )).ReturnsAsync(new GetTransferListResultDto
        {
            List = new List<TokenTransferInfoDto>()
            {
                new TokenTransferInfoDto()
                {
                    TransactionId = "0x1",
                    ChainIds = new List<string>(){EoaServerApplicationTestConstant.ChainIdTDVW},
                    BlockTime = 1
                },
                new TokenTransferInfoDto()
                {
                    TransactionId = "0x2",
                    ChainIds = new List<string>(){EoaServerApplicationTestConstant.ChainIdTDVW},
                    BlockTime = 2
                }
            }
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<GetTransferListResultDto>(
            It.Is<string>(url => 
                url.StartsWith("mockAElfScanUrl/" + CommonConstant.AelfScanUserTransfersApi) &&
                url.Contains($"chainId={EoaServerApplicationTestConstant.ChainIdTDVW}&tokenType=1&address={EoaServerApplicationTestConstant.User1Address}&skipCount=0"))
        )).ReturnsAsync(new GetTransferListResultDto
        {
            List = new List<TokenTransferInfoDto>()
            {
                new TokenTransferInfoDto()
                {
                    TransactionId = "0x3",
                    ChainIds = new List<string>(){EoaServerApplicationTestConstant.ChainIdTDVW},
                    BlockTime = 3
                }
            }
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<TransactionDetailResponseDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTransactionDetailApi}?TransactionId=0x1&ChainId={EoaServerApplicationTestConstant.ChainIdTDVW}"
        )).ReturnsAsync(new TransactionDetailResponseDto
        {
            List = new List<TransactionDetailDto>()
            {
                new TransactionDetailDto()
                {
                    TransactionId = "0x1",
                    Status = Commons.TransactionStatus.Mined,
                    Confirmed = true,
                    Method = "BatchBuyNow",
                    Timestamp = 1,
                    From = new CommonAddressDto()
                    {
                        Address = EoaServerApplicationTestConstant.User1Address
                    },
                    To = new CommonAddressDto()
                    {
                        Address = EoaServerApplicationTestConstant.ForestContractAddress
                    },
                    TokenTransferreds = new List<TokenTransferredDto>()
                    {
                        new TokenTransferredDto()
                        {
                            Symbol = EoaServerApplicationTestConstant.TokenElfSymbol,
                            Amount = 1,
                            ImageUrl = EoaServerApplicationTestConstant.TokenElfIcon,
                            From = new CommonAddressDto()
                            {
                                Address = EoaServerApplicationTestConstant.User1Address
                            },
                            To = new CommonAddressDto()
                            {
                                Address = ""
                            }
                        },
                        new TokenTransferredDto()
                        {
                            Symbol = EoaServerApplicationTestConstant.TokenElfSymbol,
                            Amount = 2,
                            ImageUrl = EoaServerApplicationTestConstant.TokenElfIcon,
                            From = new CommonAddressDto()
                            {
                                Address = EoaServerApplicationTestConstant.User1Address
                            },
                            To = new CommonAddressDto()
                            {
                                Address = ""
                            }
                        }
                    },
                    NftsTransferreds = new List<NftsTransferredDto>()
                    {
                        new NftsTransferredDto()
                        {
                            Symbol = EoaServerApplicationTestConstant.NftBBB2Symbol,
                            Amount = 1,
                            ImageUrl = EoaServerApplicationTestConstant.NftBBB2Icon,
                            From = new CommonAddressDto()
                            {
                                Address = ""
                            },
                            To = new CommonAddressDto()
                            {
                                Address = EoaServerApplicationTestConstant.User1Address
                            }
                        }
                    }
                }
            }
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<TransactionDetailResponseDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTransactionDetailApi}?TransactionId=0x2&ChainId={EoaServerApplicationTestConstant.ChainIdTDVW}"
        )).ReturnsAsync(new TransactionDetailResponseDto
        {
            List = new List<TransactionDetailDto>()
            {
                new TransactionDetailDto()
                {
                    TransactionId = "0x2",
                    Status = Commons.TransactionStatus.Mined,
                    Confirmed = true,
                    Method = "Transfer",
                    Timestamp = 2,
                    From = new CommonAddressDto()
                    {
                        Address = EoaServerApplicationTestConstant.User2Address
                    },
                    To = new CommonAddressDto()
                    {
                        Address = EoaServerApplicationTestConstant.TransferContractAddress
                    },
                    TokenTransferreds = new List<TokenTransferredDto>()
                    {
                        new TokenTransferredDto()
                        {
                            Symbol = EoaServerApplicationTestConstant.TokenElfSymbol,
                            Amount = 1,
                            ImageUrl = EoaServerApplicationTestConstant.TokenElfIcon,
                            From = new CommonAddressDto()
                            {
                                Address = EoaServerApplicationTestConstant.User2Address
                            },
                            To = new CommonAddressDto()
                            {
                                Address = EoaServerApplicationTestConstant.User1Address
                            }
                        }
                    }
                }
            }
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<TransactionDetailResponseDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTransactionDetailApi}?TransactionId=0x3&ChainId={EoaServerApplicationTestConstant.ChainIdTDVW}"
        )).ReturnsAsync(new TransactionDetailResponseDto
        {
            List = new List<TransactionDetailDto>()
            {
                new TransactionDetailDto()
                {
                    TransactionId = "0x3",
                    Status = Commons.TransactionStatus.Mined,
                    Confirmed = true,
                    Method = "Transfer",
                    Timestamp = 3,
                    From = new CommonAddressDto()
                    {
                        Address = EoaServerApplicationTestConstant.User2Address
                    },
                    To = new CommonAddressDto()
                    {
                        Address = EoaServerApplicationTestConstant.TransferContractAddress
                    },
                    NftsTransferreds = new List<NftsTransferredDto>()
                    {
                        new NftsTransferredDto()
                        {
                            Symbol = EoaServerApplicationTestConstant.NftBBB2Symbol,
                            Amount = 1,
                            ImageUrl = EoaServerApplicationTestConstant.NftBBB2Icon,
                            From = new CommonAddressDto()
                            {
                                Address = EoaServerApplicationTestConstant.User2Address
                            },
                            To = new CommonAddressDto()
                            {
                                Address = EoaServerApplicationTestConstant.User1Address
                            }
                        }
                    }
                }
            }
        });
        
    }
    
    private void MockTokenInfoData(Mock<IHttpClientProvider> mockHttpProvider)
    {
        mockHttpProvider.Setup(provider => provider.GetDataAsync<IndexerTokenInfoDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTokenInfoApi}?Symbol=ELF&ChainId=tDVW"
        )).ReturnsAsync(new IndexerTokenInfoDto
        {
            Decimals = 8,
            Symbol = EoaServerApplicationTestConstant.TokenElfSymbol
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<IndexerTokenInfoDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTokenInfoApi}?Symbol=SGR&ChainId=tDVW"
        )).ReturnsAsync(new IndexerTokenInfoDto
        {
            Decimals = 8,
            Symbol = EoaServerApplicationTestConstant.TokenSgrSymbol
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<IndexerTokenInfoDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTokenInfoApi}?Symbol=SGR&ChainId=AELF"
        )).ReturnsAsync(new IndexerTokenInfoDto
        {
            Decimals = 8,
            Symbol = EoaServerApplicationTestConstant.TokenSgrSymbol
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<IndexerTokenInfoDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTokenInfoApi}?Symbol=BBB-2&ChainId=tDVW"
        )).ReturnsAsync(new IndexerTokenInfoDto
        {
            Decimals = 0,
            Symbol = EoaServerApplicationTestConstant.NftBBB2Symbol,
            TokenName = EoaServerApplicationTestConstant.NftBBB2TokenName,
            Supply = 123,
            TotalSupply = 234
        });
        
        mockHttpProvider.Setup(provider => provider.GetDataAsync<IndexerTokenInfoDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTokenInfoApi}?Symbol=BBB-1&ChainId=tDVW"
        )).ReturnsAsync(new IndexerTokenInfoDto
        {
            Decimals = 0,
            Symbol = EoaServerApplicationTestConstant.NftBBB1Symbol,
            TokenName = EoaServerApplicationTestConstant.NftBBB2TokenName,
            Supply = 223,
            TotalSupply = 233
        });
    }
}