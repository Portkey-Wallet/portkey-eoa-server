using EoaServer.Grain.Tests;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace EoaServer;

[DependsOn(
    typeof(EoaServerApplicationModule),
    typeof(AbpEventBusModule),
    typeof(EoaServerGrainTestModule),
    typeof(EoaServerDomainTestModule)
)]
public class EoaServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<EoaServerApplicationModule>(); });
        
        base.ConfigureServices(context);
    }
}