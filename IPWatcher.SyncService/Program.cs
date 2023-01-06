using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using IPWatcher.SyncService;
using IPWatcher.AzurePersistantStorage;
using IPWatcher.IpifyClient;
using IPWatcher.SyncHandler;
using Serilog.Events;

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
        services.Configure<CronScheduleConfiguration>(hostBuilderContext.Configuration.GetSection("CronScheduleConfiguration"));
        services.Configure<AzureStorageConfiguration>(hostBuilderContext.Configuration.GetSection("AzureStorageConfiguration"));
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
