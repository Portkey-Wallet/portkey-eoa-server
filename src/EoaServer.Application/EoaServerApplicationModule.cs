﻿using EoaServer.Redis;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.DistributedLocking;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace EoaServer;

[DependsOn(
    typeof(EoaServerDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(EoaServerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(EoaServerGrainsModule),
    typeof(AbpDistributedLockingModule)
)]
public class EoaServerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<EoaServerApplicationModule>(); });
        context.Services.AddHttpClient();
        context.Services.AddSingleton<RedisClient>();
        
        var configuration = context.Services.GetConfiguration();
        //Configure<ScheduledTasksOptions>(configuration.GetSection("ScheduledTasks"));
    }
}