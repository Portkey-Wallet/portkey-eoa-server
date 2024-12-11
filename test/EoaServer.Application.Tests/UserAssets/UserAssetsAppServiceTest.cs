using System.Collections.Generic;
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
        
        result.Data.Count.ShouldBe(2);
        result.Data[0].Symbol.ShouldBe(EoaServerApplicationTestConstant.TokenElfSymbol);
        result.Data[0].ImageUrl.ShouldBe(EoaServerApplicationTestConstant.TokenElfIcon);
        result.Data[0].Balance.ShouldBe("3");
        result.Data[0].Tokens.Count.ShouldBe(2);
        result.Data[0].Tokens[0].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdTDVW);
        result.Data[0].Tokens[1].ChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdAELF);
        
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
            }
        });
        
        
    }
}