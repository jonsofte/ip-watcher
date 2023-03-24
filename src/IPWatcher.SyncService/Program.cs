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

using IPWatcher.SyncService;

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(Path.GetDirectoryName(AppContext.BaseDirectory)!)
    .ConfigureAppConfiguration((hostContext, configuration) =>
    {
        IHostEnvironment env = hostContext.HostingEnvironment;
        configuration.SetBasePath(AppContext.BaseDirectory);
        configuration.AddEnvironmentVariables("IPWatcher_");
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        bool EnableOTEL = isOTELEnabled(hostBuilderContext);
        string OTELEndoint = hostBuilderContext.Configuration["ApplicationConfiguration:OTELExporterEndpoint"]!;


        services.AddOptions();
        services.AddOpenTelemetry()
        .WithTracing(builder =>
        {
            builder
                .AddConsoleExporter()
                .AddSource("IP Watcher")
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(hostBuilderContext.HostingEnvironment.ApplicationName)
                        );
            
            if (EnableOTEL)
            {
                builder.AddOtlpExporter(o => o.Endpoint = new Uri(OTELEndoint));
                Console.WriteLine($"OTEL enabled. Sending traces to: {OTELEndoint}");
            }
            builder.Build();
        });
        services.Configure<ApplicationConfiguration>(hostBuilderContext.Configuration.GetSection("ApplicationConfiguration"));
        services.Configure<AzureStorageConfiguration>(hostBuilderContext.Configuration.GetSection("AzureStorageConfiguration"));
        services.TryAddSingleton(new ActivitySource("IP Watcher"));
        services.AddIpifyClient();
        services.AddIPStorage();
        services.AddSyncService();
        services.AddHostedService<SynchronizationCronService>();
    })
    .UseSerilog((hostBuilderContext, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(hostBuilderContext.Configuration)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        .WriteTo.Console())
    .Build();

await host.RunAsync();

static bool isOTELEnabled(HostBuilderContext hostBuilderContext)
{

    if (string.IsNullOrWhiteSpace(hostBuilderContext.Configuration["ApplicationConfiguration:OTELEnable"]) ||
        hostBuilderContext.Configuration["ApplicationConfiguration:OTELEnable"]! != "true")
    {
        Console.WriteLine("OTEL not enabled");
        return false;
    }
    return true;
}
