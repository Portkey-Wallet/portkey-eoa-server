using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using EoaServer.Entities.Es;
using Microsoft.Extensions.Logging;
using Nest;

namespace EoaServer.UserToken;

public interface IUserTokenProvider
{
    Task<List<UserTokenIndex>> GetUserTokenInfoListAsync(Guid userId, string chainId, string symbol);

}

public class UserTokenProvider : EoaServerBaseService, IUserTokenProvider
{
    private readonly ILogger<UserTokenProvider> _logger;
    private readonly INESTRepository<UserTokenIndex, Guid> _userTokenIndexRepository;

    public UserTokenProvider(
        ILogger<UserTokenProvider> logger,
        INESTRepository<UserTokenIndex, Guid> userTokenIndexRepository)
    {
        _logger = logger;
        _userTokenIndexRepository = userTokenIndexRepository;
    }

    public async Task<List<UserTokenIndex>> GetUserTokenInfoListAsync(Guid userId, string chainId, string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Token.Symbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Token.ChainId).Value(chainId)));
        QueryContainer filter(QueryContainerDescriptor<UserTokenIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var (totalCount, userTokens) = await _userTokenIndexRepository.GetSortListAsync(filter);

        if (totalCount == 0)
        {
            return new List<UserTokenIndex>();
        }

        return userTokens;
    }
    
}