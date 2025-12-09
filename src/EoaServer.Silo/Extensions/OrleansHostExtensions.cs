using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Statistics;
using Serilog;

namespace EoaServer.Silo.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansSnapshot(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            //Configure OrleansSnapshot
            var orleansConfigSection = context.Configuration.GetSection("Orleans");
            var isRunningInKubernetes = orleansConfigSection.GetValue<bool>("IsRunningInKubernetes");
            if (isRunningInKubernetes)
            {
                Log.Warning("==Use kubernetes hosting...");
                UseKubernetesHostClustering(siloBuilder, orleansConfigSection);
                Log.Warning("==Use kubernetes hosting end...");
            }
            else
            {
                Log.Warning("==Use docker hosting...");
                UseDockerHostClustering(siloBuilder, orleansConfigSection);
                Log.Warning("==Use docker hosting end...");
            }
        });
    }

    private static void UseKubernetesHostClustering(ISiloBuilder siloBuilder, IConfigurationSection configSection)
    {
        Log.Warning("==Configuration");
        Log.Warning("==  POD_IP: {0}", Environment.GetEnvironmentVariable("POD_IP"));
        Log.Warning("==  SiloPort: {0}", configSection.GetValue<int>("SiloPort"));
        Log.Warning("==  GatewayPort: {0}", configSection.GetValue<int>("GatewayPort"));
        Log.Warning("==  DatabaseName: {0}", configSection.GetValue<string>("DataBase"));
        Log.Warning("==  ClusterId: {0}", Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID"));
        Log.Warning("==  ServiceId: {0}", Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID"));
        Log.Warning("==Configuration");

        //Configure OrleansSnapshot
        siloBuilder
            .ConfigureEndpoints(
                advertisedIP: IPAddress.Parse(Environment.GetEnvironmentVariable("POD_IP") ?? string.Empty),
                siloPort: configSection.GetValue<int>("SiloPort"),
                gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
            .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
            })
            .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings =
                settings =>
                {
                    settings.NullValueHandling = NullValueHandling.Include;
                    settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    settings.DefaultValueHandling = DefaultValueHandling.Populate;
                })
            .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
            {
                op.CollectionPrefix = "GrainStorage";
                op.DatabaseName = configSection.GetValue<string>("DataBase");

                var grainIdPrefix = configSection
                    .GetSection("GrainSpecificIdPrefix").GetChildren().ToDictionary(o => o.Key.ToLower(), o => o.Value);
                op.KeyGenerator = id =>
                {
                    var grainType = id.Type.ToString();
                    if (grainIdPrefix.TryGetValue(grainType, out var prefix))
                    {
                        return prefix.StartsWith("GrainReference=000000") ? $"{prefix}+{id.Key}" : prefix;
                    }

                    return id.ToString();
                };
                op.CreateShardKeyForCosmos = configSection.GetValue<bool>("CreateShardKeyForMongoDB", false);
            })
            .UseMongoDBReminders(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.CreateShardKeyForCosmos = false;
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID");
                options.ServiceId = Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID");
            })
            // .AddMemoryGrainStorage("PubSubStore")
            .UseDashboard(options =>
            {
                options.Username = configSection.GetValue<string>("DashboardUserName");
                options.Password = configSection.GetValue<string>("DashboardPassword");
                options.Host = "*";
                options.Port = configSection.GetValue<int>("DashboardPort");
                options.HostSelf = true;
                options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
            })
            .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
    }

    private static void UseDockerHostClustering(ISiloBuilder siloBuilder, IConfigurationSection configSection)
    {
        siloBuilder
            .ConfigureEndpoints(advertisedIP: IPAddress.Parse(configSection.GetValue<string>("AdvertisedIP")),
                siloPort: configSection.GetValue<int>("SiloPort"),
                gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
            .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
            })
            .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings =
                settings =>
                {
                    settings.NullValueHandling = NullValueHandling.Include;
                    settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    settings.DefaultValueHandling = DefaultValueHandling.Populate;
                })
            .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
            {
                op.CollectionPrefix = "GrainStorage";
                op.DatabaseName = configSection.GetValue<string>("DataBase");

                var grainIdPrefix = configSection
                    .GetSection("GrainSpecificIdPrefix").GetChildren().ToDictionary(o => o.Key.ToLower(), o => o.Value);
                op.KeyGenerator = id =>
                {
                    var grainType = id.Type.ToString();
                    if (grainIdPrefix.TryGetValue(grainType, out var prefix))
                    {
                        return prefix.StartsWith("GrainReference=000000") ? $"{prefix}+{id.Key}" : prefix;
                    }

                    return id.ToString();
                };
                op.CreateShardKeyForCosmos = configSection.GetValue<bool>("CreateShardKeyForMongoDB", false);
            })
            .UseMongoDBReminders(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.CreateShardKeyForCosmos = false;
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = configSection.GetValue<string>("ClusterId");
                options.ServiceId = configSection.GetValue<string>("ServiceId");
            })
            // .AddMemoryGrainStorage("PubSubStore")
            .UseDashboard(options =>
            {
                options.Username = configSection.GetValue<string>("DashboardUserName");
                options.Password = configSection.GetValue<string>("DashboardPassword");
                options.Host = "*";
                options.Port = configSection.GetValue<int>("DashboardPort");
                options.HostSelf = true;
                options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
            })
            .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
    }
}