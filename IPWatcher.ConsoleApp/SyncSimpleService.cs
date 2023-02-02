using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IPWatcher.Abstractions.Interfaces;
using Microsoft.Extensions.Options;
using System;

namespace IPWatcher.ConsoleApp;

public class SyncHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IIpSyncService _syncService;
    private readonly ApplicationConfiguration _configuration;

    public SyncHostedService(ILogger<SyncHostedService> logger, 
        IHostApplicationLifetime appLifetime,
        IOptionsMonitor<ApplicationConfiguration> configuration,
        IIpSyncService iPSyncService)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _syncService = iPSyncService;
        _configuration = configuration.CurrentValue;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var appVersion = !string.IsNullOrEmpty(_configuration.Version) ? _configuration.Version: "unknown";
        _logger.LogInformation("{applicationName} Version: {applicationVersion}", assembly.GetName().Name, appVersion);
        _appLifetime.ApplicationStarted.Register(() =>
        {

            Task.Run(async () =>
            {
                try
                {
                    await _syncService.StartSync(cancellationToken);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception");
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            });
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
