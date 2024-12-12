using System.Collections.Generic;
using EoaServer.UserActivity.Dto;
using EoaServer.UserActivity.Dtos;
using EoaServer.UserAssets;
using Shouldly;
using Xunit;

namespace EoaServer.UserActivity;

public class UserActivityAppServiceTest : EoaServerApplicationTestBase
{
    private readonly IUserActivityAppService _userActivityAppService;
    
    public UserActivityAppServiceTest()
    {
        _userActivityAppService = GetRequiredService<IUserActivityAppService>();
    }

    [Fact]
    public async void GetActivityAsyncTest()
    {
        var result = await _userActivityAppService.GetActivityAsync(new GetActivityRequestDto()
        {
            TransactionId = "0x1",
            ChainId = "tDVW"
        });
        result.TransactionId.ShouldBe("0x1");
        result.FromAddress.ShouldBe(EoaServerApplicationTestConstant.User1Address);
        result.ToAddress.ShouldBe(EoaServerApplicationTestConstant.ForestContractAddress);
        result.FromChainId.ShouldBe(EoaServerApplicationTestConstant.ChainIdTDVW);
        result.FromChainIdUpdated.ShouldBe("aelf dAppChain");
        result.FromChainIcon.ShouldBe("https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/aelf/dappChain.png");
        result.DappName.ShouldBe("Forest");
        result.Operations.ShouldNotBeNull();
        result.Operations.Count.ShouldBe(2);
        result.Operations[0].Amount.ShouldBe("3");
        result.Operations[0].Symbol.ShouldBe("ELF");
        result.Operations[0].Icon.ShouldBe("ELF_ImageUrl");
        result.Operations[0].IsReceived.ShouldBeFalse();
        result.Operations[1].Amount.ShouldBe("1");
        result.Operations[1].Symbol.ShouldBe("BBB-2");
        result.Operations[1].NftInfo.ImageUrl.ShouldBe("BBB-2_ImageUrl");
        result.Operations[1].NftInfo.Alias.ShouldBe("xxx2");
        result.Operations[1].NftInfo.NftId.ShouldBe("2");
        result.Operations[1].IsReceived.ShouldBeTrue();
    }
    
    [Fact]
    public async void GetActivitiesAsyncTest()
    {
        var result = await _userActivityAppService.GetActivitiesAsync(new GetActivitiesRequestDto()
        {
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW
                }
            }
        });
        
        result.Data.Count.ShouldBe(3);
        result.Data[0].TransactionId.ShouldBe("0x3");
        result.Data[1].TransactionId.ShouldBe("0x2");
        result.Data[2].TransactionId.ShouldBe("0x1");
    }
    
    [Fact]
    public async void GetActivitiesAsyncSkipTest()
    {
        var result = await _userActivityAppService.GetActivitiesAsync(new GetActivitiesRequestDto()
        {
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo()
                {
                    Address = EoaServerApplicationTestConstant.User1Address,
                    ChainId = EoaServerApplicationTestConstant.ChainIdTDVW
                },
            },
            SkipCount = 1,
            MaxResultCount = 1
        });
        
        result.Data.Count.ShouldBe(1);
        result.Data[0].TransactionId.ShouldBe("0x2");
    }
}