using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EoaServer.Security;
using EoaServer.Token;
using EoaServer.Token.Request;
using EoaServer.UserToken.Request;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace EoaServer.UserToken;


public class TokenAppServiceTest : EoaServerApplicationTestBase
{
    private readonly ITokenAppService _tokenAppService;
    protected ICurrentUser _currentUser;

    public TokenAppServiceTest()
    {
        _tokenAppService = GetRequiredService<ITokenAppService>();
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
        var result = await _tokenAppService.GetTokenListAsync(new GetTokenListRequestDto
        {
            Symbol = "el",
            ChainIds = new List<string>()
            {
                "tDVW",
                "AELF"
            }
        });
        result.Count.ShouldBe(2);
        
    }
}