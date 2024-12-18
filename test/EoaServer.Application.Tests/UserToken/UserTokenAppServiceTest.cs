using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EoaServer.Security;
using EoaServer.UserToken.Request;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace EoaServer.UserToken;


public class UserTokenAppServiceTest : EoaServerApplicationTestBase
{
    private readonly IUserTokenAppService _userTokenAppService;
    protected ICurrentUser _currentUser;

    public UserTokenAppServiceTest()
    {
        _userTokenAppService = GetRequiredService<IUserTokenAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        services.AddSingleton(_currentUser);
    }

    [Fact]
    public async Task ChangeTokenDisplayAsyncTest()
    {
        await _userTokenAppService.ChangeTokenDisplayAsync("tDVW-ETH", true);
        await _userTokenAppService.ChangeTokenDisplayAsync("AELF-ETH", true);
        await _userTokenAppService.ChangeTokenDisplayAsync("tDVW-SGR-1", true);
        await _userTokenAppService.ChangeTokenDisplayAsync("AELF-SGR-1", true);
        Thread.Sleep(2000);
        
        var userTokens = await _userTokenAppService.GetTokensAsync(new GetTokenInfosRequestDto());
        userTokens.TotalCount.ShouldBe(6);
        userTokens.Items.Count.ShouldBe(6);
        userTokens.Items[0].Symbol.ShouldBe("ELF");
        userTokens.Items[0].ChainId.ShouldBe("tDVW");
        userTokens.Items[1].Symbol.ShouldBe("ELF");
        userTokens.Items[1].ChainId.ShouldBe("AELF");
        userTokens.Items[4].Symbol.ShouldBe("SGR-1");
        userTokens.Items[4].Label.ShouldBe("SGR");
        userTokens.Items[4].IsDisplay.ShouldBe(true);
        userTokens.Items[4].ChainId.ShouldBe("tDVW");
        userTokens.Items[5].Symbol.ShouldBe("SGR-1");
        userTokens.Items[5].Label.ShouldBe("SGR");
        userTokens.Items[5].IsDisplay.ShouldBe(true);
        userTokens.Items[5].ChainId.ShouldBe("AELF");
        
        await _userTokenAppService.ChangeTokenDisplayAsync("tDVW-SGR-1", false);
        await _userTokenAppService.ChangeTokenDisplayAsync("AELF-SGR-1", false);
        Thread.Sleep(2000);
        
        userTokens = await _userTokenAppService.GetTokensAsync(new GetTokenInfosRequestDto());
        userTokens.TotalCount.ShouldBe(6);
        userTokens.Items.Count.ShouldBe(6);
        userTokens.Items[4].Symbol.ShouldBe("SGR-1");
        userTokens.Items[4].IsDisplay.ShouldBe(false);
        userTokens.Items[5].Symbol.ShouldBe("SGR-1");
        userTokens.Items[5].IsDisplay.ShouldBe(false);
        
        userTokens = await _userTokenAppService.GetTokensAsync(new GetTokenInfosRequestDto()
        {
            Keyword = "el",
            ChainIds = new List<string>()
            {
                "tDVW"
            }
        });
        userTokens.TotalCount.ShouldBe(1);
        userTokens.Items.Count.ShouldBe(1);
        userTokens.Items[0].Symbol.ShouldBe("ELF");
        userTokens.Items[0].ChainId.ShouldBe("tDVW");
    }
}