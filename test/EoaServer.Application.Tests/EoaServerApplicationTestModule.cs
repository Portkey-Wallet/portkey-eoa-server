using System.Collections.Generic;
using EoaServer.Common;
using EoaServer.Commons;
using EoaServer.Grain.Tests;
using EoaServer.Options;
using EoaServer.Token.Dto;
using EoaServer.UserActivity;
using EoaServer.UserActivity.Dto;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace EoaServer;

[DependsOn(
    typeof(EoaServerApplicationModule),
    typeof(AbpEventBusModule),
    typeof(EoaServerGrainTestModule),
    typeof(EoaServerDomainTestModule)
)]
public class EoaServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<EoaServerApplicationModule>(); });
        
        base.ConfigureServices(context);
        
        var mockHttpProvider = new Mock<IHttpClientProvider>();
        SetupActivitiesMockData(mockHttpProvider);
        context.Services.AddSingleton<IHttpClientProvider>(mockHttpProvider.Object);
        
        Configure<ActivityOptions>(options =>
        {
            
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
                        IsMainChain = true
                    }
                },
                {
                    EoaServerApplicationTestConstant.ChainIdTDVW, new ChainInfo()
                    {
                        ChainId = EoaServerApplicationTestConstant.ChainIdTDVW,
                        IsMainChain = false
                    }
                },
            };
        });

        Configure<AElfScanOptions>(options =>
        {
            options.BaseUrl = "mockAElfScanUrl";
        });
        
        context.Services.AddSingleton<IUserActivityAppService, UserActivityAppService>();
    }

    private void SetupActivitiesMockData(Mock<IHttpClientProvider> mockHttpProvider)
    {
        mockHttpProvider.Setup(provider => provider.GetAsync<IndexerTokenInfoDto>(
            "tokenInfo",
            new Dictionary<string, string>()
            {
                { "Symbol", "ELF" },
                { "ChainId", "tDVW" }
            }
        )).ReturnsAsync(new IndexerTokenInfoDto
        {
            Decimals = 8,
            Symbol = "ELF"
        });
        
        mockHttpProvider.Setup(provider => provider.GetAsync<IndexerTokenInfoDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTokenInfoApi}",
            new Dictionary<string, string>()
            {
                { "Symbol", "BBB-2" },
                { "ChainId", "tDVW" }
            }
        )).ReturnsAsync(new IndexerTokenInfoDto
        {
            Decimals = 0,
            Symbol = "BBB-2",
            TokenName = "cnjsdb"
        });
        
        mockHttpProvider.Setup(provider => provider.GetAsync<TransactionDetailResponseDto>(
            $"mockAElfScanUrl/{CommonConstant.AelfScanTransactionDetailApi}",
            new Dictionary<string, string>()
            {
                { "TransactionId", "0x1" },
                { "ChainId", "tDVW" }
            }
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
                    Timestamp = 1732963652,
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
                            Symbol = "ELF",
                            Amount = 1,
                            ImageUrl = "ELF_ImageUrl",
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
                            Symbol = "ELF",
                            Amount = 2,
                            ImageUrl = "ELF_ImageUrl",
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
                            Symbol = "BBB-2",
                            Amount = 1,
                            ImageUrl = "BBB-2_ImageUrl",
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
    }
}