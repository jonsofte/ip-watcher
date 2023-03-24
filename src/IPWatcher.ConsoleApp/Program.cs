using System.Reflection;
using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;

using IPWatcher.AzurePersistantStorage;
using IPWatcher.SyncHandler;
using IPWatcher.IpifyClient;
using IPWatcher.ConsoleApp;

await CreateHostBuilder(args).RunConsoleAsync();

static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
    .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
    .ConfigureAppConfiguration((hostContext, configuration) =>
    {
        IHostEnvironment env = hostContext.HostingEnvironment;
        configuration.AddEnvironmentVariables("IPWatcher_");
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        bool EnableOTEL = isOTELEnabled(hostBuilderContext);
        string OTELEndoint = hostBuilderContext.Configuration["ApplicationConfiguration:OTELExporterEndpoint"]!;

        services.AddOptions();
        services.AddOpenTelemetry()
        .WithTracing( builder =>
            {
                if (EnableOTEL) 
                { 
                    builder.AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(OTELEndoint);
                            o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                        }
                    );
                }

                builder
                    .AddConsoleExporter()
                    .AddSource("IP Watcher")
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(hostBuilderContext.HostingEnvironment.ApplicationName)
                            );
             });
        services.Configure<ApplicationConfiguration>(hostBuilderContext.Configuration.GetSection("ApplicationConfiguration"));
        services.Configure<AzureStorageConfiguration>(hostBuilderContext.Configuration.GetSection("AzureStorageConfiguration"));
        services.TryAddSingleton(new ActivitySource("IP Watcher"));
        services.AddIpifyClient();
        services.AddIPStorage();
        services.AddSyncService();
        services.AddHostedService<SyncHostedService>();
    })
    .UseSerilog((hostBuilderContext, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(hostBuilderContext.Configuration)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        .WriteTo.Console()
        );

static bool isOTELEnabled(HostBuilderContext hostBuilderContext)
{
    if (string.IsNullOrWhiteSpace(hostBuilderContext.Configuration["ApplicationConfiguration:OTELEnable"]))
    {
        return false;
    }
    return (hostBuilderContext.Configuration["ApplicationConfiguration:OTELEnable"]! == "true");
}
