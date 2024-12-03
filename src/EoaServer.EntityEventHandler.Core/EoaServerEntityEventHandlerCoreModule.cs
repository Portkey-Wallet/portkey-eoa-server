using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace EoaServer.EntityEventHandler.Core
{
    [DependsOn(typeof(AbpAutoMapperModule), typeof(EoaServerApplicationModule),
        typeof(EoaServerApplicationContractsModule))]
    public class EoaServerEntityEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                //Add all mappings defined in the assembly of the MyModule class
                options.AddMaps<EoaServerEntityEventHandlerCoreModule>();
            });
        }
    }
}