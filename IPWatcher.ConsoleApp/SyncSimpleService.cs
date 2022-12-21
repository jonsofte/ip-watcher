using System;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IPWatcher.Abstractions.Interfaces;

namespace IPWatcher.ConsoleApp;

public class SyncHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IIPSyncService _syncService;

    public SyncHostedService(ILogger<SyncHostedService> logger, IHostApplicationLifetime appLifetime, IIPSyncService iPSyncService)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _syncService = iPSyncService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        _logger.LogDebug("{applicationName} Version: {applicationVersion}", assembly.GetName().Name, fileVersionInfo.ProductVersion);
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
