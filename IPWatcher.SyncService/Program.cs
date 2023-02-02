using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using IPWatcher.SyncService;
using IPWatcher.AzurePersistantStorage;
using IPWatcher.IpifyClient;
using IPWatcher.SyncHandler;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddOptions();
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder.AddConsoleExporter()
                .AddSource("IPWatcher")
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(hostBuilderContext.HostingEnvironment.ApplicationName))
                ;
            });
        services.Configure<ApplicationConfiguration>(hostBuilderContext.Configuration.GetSection("ApplicationConfiguration"));
        services.Configure<AzureStorageConfiguration>(hostBuilderContext.Configuration.GetSection("AzureStorageConfiguration"));
        services.TryAddSingleton(Sdk.CreateTracerProviderBuilder().Build()!);
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
