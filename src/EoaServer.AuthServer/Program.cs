using System;
using System.Threading.Tasks;
using EoaServer;
using EoaServer.AuthServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

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
            Log.Information("Starting EoaServer.AuthServer.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.AddAppSettingsSecretsJson()
                // .InitAppConfiguration(false)
                // .UseApolloForConfigureHostBuilder()
                .UseAutofac()
                .UseSerilog();

            await builder.AddApplicationAsync<EoaServerAuthServerModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "EoaServer.AuthServer terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static ILogger CreateLogger(LogEventLevel logEventLevel)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(logEventLevel)
            .Enrich.FromLogContext()
            .WriteTo.Async(c =>
                c.Console(
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}"))
            .WriteTo.Async(c => c.File("Logs/log-.log", rollingInterval: RollingInterval.Day,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}"))
            .CreateLogger();
    }
}

