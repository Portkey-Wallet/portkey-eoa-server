using System.Collections.Generic;
using System.Linq;
using DeviceDetectorNET;
using Shouldly;
using Xunit;

namespace EoaServer.UserAssets;


public class UserAssetsAppServiceTest : EoaServerApplicationTestBase
{
    private readonly IUserAssetsAppService _userAssetsAppService;

    public UserAssetsAppServiceTest()
    {
        _userAssetsAppService = GetRequiredService<IUserAssetsAppService>();
    }

    [Fact]
    public async void GetTokenAsyncTest()
    {
        var result = await _userAssetsAppService.GetTokenAsync(new GetTokenRequestDto()
        {
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW
                },
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdAELF
                }
            }
        });
        
        result.TotalRecordCount.ShouldBe(3);
        result.Data.Count.ShouldBe(3);
        result.Data[0].Symbol.ShouldBe("ELF");
        result.Data[0].ImageUrl.ShouldBe(EoaServerApplicationTestConstant.TokenElfIcon);
        result.Data[0].Balance.ShouldBe("3");
        result.Data[0].Tokens.Count.ShouldBe(2);
        result.Data[0].Tokens[0].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdTDVW);
        result.Data[0].Tokens[0].Balance.ShouldBe("1");
        result.Data[0].Tokens[1].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdAELF);
        result.Data[0].Tokens[1].Balance.ShouldBe("2");
        result.Data[1].Symbol.ShouldBe("SGR");
        result.Data[1].Tokens.Count.ShouldBe(2);
        result.Data[1].Balance.ShouldBe("1");
        result.Data[1].Tokens[0].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdTDVW);
        result.Data[1].Tokens[0].Balance.ShouldBe("1");
        result.Data[1].Tokens[1].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdAELF);
        result.Data[1].Tokens[1].Balance.ShouldBe("0");
        // default show
        result.Data[2].Symbol.ShouldBe("ETH");
        result.Data[2].Tokens.Count.ShouldBe(2);
        result.Data[2].Tokens[0].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdTDVW);
        result.Data[2].Tokens[0].Balance.ShouldBe("0");
        result.Data[2].Tokens[1].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdAELF);
        result.Data[2].Tokens[1].Balance.ShouldBe("0");
        
        
        result = await _userAssetsAppService.GetTokenAsync(new GetTokenRequestDto()
        {
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW
                },
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdAELF
                }
            },
            SkipCount = 1
        });
        result.TotalRecordCount.ShouldBe(3);
        result.Data.Count.ShouldBe(2);
        result.Data[0].Symbol.ShouldBe("SGR");
        result.Data[1].Symbol.ShouldBe("ETH");
    }
    
    [Fact]
    public async void GetNFTCollectionsAsyncTest()
    {
        var result = await _userAssetsAppService.GetNFTCollectionsAsync(new GetNftCollectionsRequestDto()
        {
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW
                },
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdAELF
                }
            }
        });
        
        result.Data.Count.ShouldBe(1);
        result.TotalRecordCount.ShouldBe(1);
        result.TotalNftItemCount.ShouldBe(2);
        result.Data[0].Symbol.ShouldBe("BBB-0");
        result.Data[0].CollectionName.ShouldBe("xxx");
        result.Data[0].ImageUrl.ShouldBe("BBB-0_ImageUrl");
        result.Data[0].ItemCount.ShouldBe(2);
        result.Data[0].IsSeed.ShouldBe(false);
        result.Data[0].DisplayChainImage.ShouldBe(false);
        result.Data[0].DisplayChainName.ShouldBe("aelf dAppChain");
        result.Data[0].ChainImageUrl.ShouldBe("https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/aelf/dappChain.png");
        result.Data[0].ChainId.ShouldBe("tDVW");
        
        
        result = await _userAssetsAppService.GetNFTCollectionsAsync(new GetNftCollectionsRequestDto()
        {
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW
                },
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdAELF
                }
            },
            SkipCount = 1
        });
        result.Data.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetNFTItemsAsyncTest()
    {
        var result = await _userAssetsAppService.GetNFTItemsAsync(new GetNftItemsRequestDto()
        {
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW
                },
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdAELF
                }
            },
            Symbol = EoaServerApplicationTestConstant.NftBBBCollectionSymbol
        });
        
        result.Data.Count.ShouldBe(2);
        result.Data[0].Symbol.ShouldBe(EoaServerApplicationTestConstant.NftBBB2Symbol);
        result.Data[0].Alias.ShouldBe(EoaServerApplicationTestConstant.NftBBB2TokenName);
        result.Data[0].Balance.ShouldBe("1");
        result.Data[0].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdTDVW);
        result.Data[0].CirculatingSupply.ShouldBe(123);
        result.Data[0].TotalSupply.ShouldBe(234);
        result.Data[0].CollectionSymbol.ShouldBe(EoaServerApplicationTestConstant.NftBBBCollectionSymbol);
        result.Data[0].TokenContractAddress.ShouldBe("ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx");
        result.Data[0].RecommendedRefreshSeconds.ShouldBe(30);
        result.Data[1].Symbol.ShouldBe(EoaServerApplicationTestConstant.NftBBB1Symbol);
        result.Data[1].Alias.ShouldBe(EoaServerApplicationTestConstant.NftBBB1TokenName);
        
        
        result = await _userAssetsAppService.GetNFTItemsAsync(new GetNftItemsRequestDto()
        {
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW
                },
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdAELF
                }
            },
            Symbol = EoaServerApplicationTestConstant.NftBBBCollectionSymbol,
            SkipCount = 1
        });
        result.Data.Count.ShouldBe(1);
    }
}