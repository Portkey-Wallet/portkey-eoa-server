using EoaServer.UserActivity.Dto;
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
        result.DappName.ShouldBe("Forest");
        result.Operations.Count.ShouldBe(2);
        result.Operations[0].Amount.ShouldBe("3");
        result.Operations[0].Symbol.ShouldBe("ELF");
        result.Operations[0].Icon.ShouldBe("");
        result.Operations[0].IsReceived.ShouldBeFalse();
        result.Operations[1].Amount.ShouldBe("1");
        result.Operations[1].Symbol.ShouldBe("BBB-2");
        result.Operations[1].Icon.ShouldBe("");
        result.Operations[1].IsReceived.ShouldBeTrue();
    }
}