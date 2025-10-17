using Volo.Abp.Modularity;

namespace EoaServer.MongoDb;

[DependsOn(
    typeof(EoaServerTestBaseModule),
    typeof(EoaServerMongoDbModule)
    )]
public class EoaServerMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // var stringArray = IMMongoDbFixture.ConnectionString.Split('?');
        // var connectionString = stringArray[0].EnsureEndsWith('/') +
        //                            "Db_" +
        //                        Guid.NewGuid().ToString("N") + "/?" + stringArray[1];
        //
        // Configure<AbpDbConnectionOptions>(options =>
        // {
        //     options.ConnectionStrings.Default = connectionString;
        // });
    }
}
