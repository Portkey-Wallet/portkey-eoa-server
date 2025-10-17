using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using EoaServer.Entities.Es;
using EoaServer.Grain.User;
using EoaServer.User;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace EoaServer.EntityEventHandler.Core;

public class UserInfoHandler : IDistributedEventHandler<UserEto>,
    ITransientDependency
{
    private readonly ILogger<UserInfoHandler> _logger;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;

    public UserInfoHandler(ILogger<UserInfoHandler> logger, INESTRepository<UserIndex, Guid> userRepository,
        IObjectMapper objectMapper, IClusterClient clusterClient)
    {
        _logger = logger;
        _userRepository = userRepository;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
    }

    public async Task HandleEventAsync(UserEto eventData)
    {
        try
        {
            _logger.LogInformation("create user info, userId:{0}, address:{address}",
                JsonConvert.SerializeObject(eventData), eventData.Address);
            var grain = _clusterClient.GetGrain<IUserGrain>(eventData.UserId);
            var result = await grain.Create(_objectMapper.Map<UserEto, UserGrainDto>(eventData));
            if (!result.Success())
            {
                _logger.LogError("create user info fail, userInfo:{0}", JsonConvert.SerializeObject(eventData));
                return;
            }

            await _userRepository.AddAsync(_objectMapper.Map<UserEto, UserIndex>(eventData));
            _logger.LogInformation("create user info success, userId:{0}, address:{address}",
                JsonConvert.SerializeObject(eventData), eventData.Address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "create user info error, userInfo:{0}", JsonConvert.SerializeObject(eventData));
        }
    }
}