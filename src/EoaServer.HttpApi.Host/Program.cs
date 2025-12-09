using System;
using System.Threading.Tasks;
using EoaServer.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace EoaServer;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();
        
        try
        {
            Log.Information("Starting EoaServer.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("seedurl.json");
            builder.Configuration.AddJsonFile("activity.json");
            builder.Configuration.AddJsonFile("userToken.json");
            
            builder.Host.AddAppSettingsSecretsJson()
                .UseApolloForConfigureHostBuilder()
                .UseAutofac()
                // .UseOrleansClient()
                .UseSerilog();

            await builder.AddApplicationAsync<EoaServerHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
