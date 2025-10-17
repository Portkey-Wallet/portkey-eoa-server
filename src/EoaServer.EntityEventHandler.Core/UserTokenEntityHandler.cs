using EoaServer.Entities.Es;
using EoaServer.Token.Eto;

namespace EoaServer.EntityEventHandler.Core;

using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.EventBus.Distributed;


public class UserTokenEntityHandler : EntityHandlerBase,
    IDistributedEventHandler<UserTokenEto>
{
    private readonly INESTRepository<UserTokenIndex, Guid> _userTokenIndexRepository;
    private readonly ILogger<UserTokenEntityHandler> _logger;

    public UserTokenEntityHandler(INESTRepository<UserTokenIndex, Guid> userTokenIndexRepository,
        ILogger<UserTokenEntityHandler> logger)
    {
        _userTokenIndexRepository = userTokenIndexRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(UserTokenEto eventData)
    {
        _logger.LogInformation($"user token is adding. {JsonConvert.SerializeObject(eventData)}");
        
        var index = ObjectMapper.Map<UserTokenEto, UserTokenIndex>(eventData);
        await _userTokenIndexRepository.AddOrUpdateAsync(index);
        
        _logger.LogInformation($"user token add success. {JsonConvert.SerializeObject(index)}");
    }
}