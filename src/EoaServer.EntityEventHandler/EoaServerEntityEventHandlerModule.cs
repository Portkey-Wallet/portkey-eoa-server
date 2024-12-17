using AElf.Indexing.Elasticsearch.Options;
using Confluent.Kafka;
using EoaServer.Commons;
using Medallion.Threading;
using Medallion.Threading.Redis;
using EoaServer.EntityEventHandler.Core;
using EoaServer.MongoDb;
using EoaServer.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.Kafka;
using Volo.Abp.Kafka;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;

namespace EoaServer.EntityEventHandler;

[DependsOn(typeof(AbpAutofacModule),
    typeof(EoaServerMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(EoaServerEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpEventBusKafkaModule))]
public class EoaServerEntityEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureTokenCleanupService();
        context.Services.AddHostedService<EoaServerHostedService>();
        ConfigureCache(configuration);
        ConfigureEsIndexCreation();
        ConfigureDistributedLocking(context, configuration);
        ConfigureKafka(context, configuration);
        Configure<TokenPriceWorkerOption>(configuration.GetSection("TokenPriceWorker"));
    }

    private void ConfigureDistributedLocking(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer
                .Connect(configuration["Redis:Configuration"]);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });
    }
    
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options =>
        {
            options.KeyPrefix = "EoaServer:";
            options.GlobalCacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
            };
        });
    }

    //Create the ElasticSearch Index based on Domain Entity
    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(EoaServerDomainModule)); });
    }

    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    
    private void ConfigureKafka(ServiceConfigurationContext context, IConfiguration configuration)
    {
        Configure<AbpKafkaOptions>(options =>
        {
            options.Connections.Default.BootstrapServers = configuration.GetValue<string>("Kafka:Connections:Default:BootstrapServers");
            //options.Connections.Default.SaslUsername = "user";
            //options.Connections.Default.SaslPassword = "pwd";
            options.ConfigureConsumer = config =>
            {
                config.SocketTimeoutMs = configuration.GetValue<int>("Kafka:Consumer:SocketTimeoutMs");
                config.Acks = Acks.All;
                config.GroupId = configuration.GetValue<string>("Kafka:EventBus:GroupId");
                config.EnableAutoCommit = true;
                config.AutoCommitIntervalMs = configuration.GetValue<int>("Kafka:Consumer:AutoCommitIntervalMs");
            };
            options.ConfigureTopic = topic =>
            {
                topic.Name = configuration.GetValue<string>("Kafka:EventBus:TopicName");
                topic.ReplicationFactor = -1;
                topic.NumPartitions = 1;
            };
        });
    }
}