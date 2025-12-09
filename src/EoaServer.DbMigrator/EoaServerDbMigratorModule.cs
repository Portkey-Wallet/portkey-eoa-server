using EoaServer.MongoDb;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace EoaServer.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(EoaServerMongoDbModule),
    typeof(EoaServerApplicationContractsModule)
    )]
public class EoaServerDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}
