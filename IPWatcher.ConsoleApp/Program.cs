using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

using IPWatcher.AzurePersistantStorage;
using IPWatcher.SyncHandler;
using IPWatcher.IpifyClient; 
using IPWatcher.ConsoleApp;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

await CreateHostBuilder(args).RunConsoleAsync();

static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
    .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
    .ConfigureAppConfiguration((hostContext, configuration) =>
    {
        IHostEnvironment env = hostContext.HostingEnvironment;
        configuration.AddEnvironmentVariables("IPWatcher_");
        //configuration
        //    .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
        //    .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {

        services.AddOptions();
        services.Configure<AzureStorageConfiguration>(hostBuilderContext.Configuration.GetSection("AzureStorageConfiguration"));
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
        ) ;