using System;
using System.IO;
using System.Linq;
using EoaServer.AuthServer.Options;
using EoaServer.Localization;
using EoaServer.MongoDb;
using EoaServer.Options;
using Localization.Resources.AbpUi;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Kafka;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.Uow;
using Volo.Abp.VirtualFileSystem;

namespace EoaServer;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(EoaServerApplicationContractsModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpDistributedLockingModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(EoaServerMongoDbModule),
    typeof(EoaServerDomainModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusKafkaModule)
    //typeof(AOPExceptionModule)
)]
public class EoaServerAuthServerModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddServer(options =>
            {
                options.UseAspNetCore().DisableTransportSecurityRequirement();
                options.SetIssuer(new Uri(configuration["AuthServer:IssuerUri"]));
                int.TryParse(configuration["ExpirationHour"], out int expirationHour);
                if (expirationHour > 0)
                {
                    options.SetAccessTokenLifetime(DateTime.Now.AddHours(expirationHour) - DateTime.Now);
                }
            });

            builder.AddValidation(options =>
            {
                options.AddAudiences("EoaServer");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            builder.Configure(openIddictServerOptions => { openIddictServerOptions.GrantTypes.Add("signature"); });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });

        context.Services.Configure<ChainOptions>(configuration.GetSection("Chains"));
        context.Services.Configure<TimeRangeOption>(option =>
        {
            option.TimeRange = Convert.ToInt32(configuration["TimeRange"]);
        });

        Configure<AbpOpenIddictExtensionGrantsOptions>(options =>
        {
            options.Grants.Add("signature", new SignatureGrantHandler());
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<EoaServerResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );

            options.Languages.Add(new LanguageInfo("en", "en", "English"));
        });

        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle => { bundle.AddFiles("/global-styles.css"); }
            );
        });

        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = false;
            //options.IsEnabledForGetRequests = true;
            //options.ApplicationName = "AuthServer";
        });

        // if (hostingEnvironment.IsDevelopment())
        // {
        //     Configure<AbpVirtualFileSystemOptions>(options =>
        //     {
        //         options.FileSets.ReplaceEmbeddedByPhysical<EoaServerDomainSharedModule>(
        //             Path.Combine(hostingEnvironment.ContentRootPath,
        //                 $"..{Path.DirectorySeparatorChar}EoaServer.Domain.Shared"));
        //         options.FileSets.ReplaceEmbeddedByPhysical<EoaServerDomainModule>(
        //             Path.Combine(hostingEnvironment.ContentRootPath,
        //                 $"..{Path.DirectorySeparatorChar}EoaServer.Domain"));
        //     });
        // }

        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"].Split(','));

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });

        Configure<AbpBackgroundJobOptions>(options => { options.IsJobExecutionEnabled = false; });

        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "EoaServer:"; });

        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("EoaServer");
        if (!hostingEnvironment.IsDevelopment())
        {
            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "EoaServer-Protection-Keys");
        }

        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer
                .Connect(configuration["Redis:Configuration"]);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });

        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        context.Services.AddHttpClient();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        app.UseUnitOfWork();
        app.UseAuthorization();
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();

        //ConfigurationProvidersHelper.DisplayConfigurationProviders(context);
    }
}