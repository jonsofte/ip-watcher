using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

using IPWatcher.SyncHandler;

namespace IPWatcher.SyncService;

public class SynchronizationCronService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IpSyncHandler _syncHandler;
    private readonly ILogger<SynchronizationCronService> _logger;
    private readonly IOptionsMonitor<ApplicationConfiguration> _configuration;
    private readonly int _taskDelay = 1000 * 5;
    private readonly string _version;
    private CrontabSchedule _schedule;
    private string _cronScheduleString;
    private DateTime _nextRun;

    public SynchronizationCronService(
        IHostApplicationLifetime appLifetime,
        IOptionsMonitor<ApplicationConfiguration> configuration,
        ILogger<SynchronizationCronService> logger,
        IpSyncHandler syncHandler)
    {
        _configuration = configuration;
        _logger = logger;
        _syncHandler = syncHandler;
        _appLifetime = appLifetime;
        

        if (string.IsNullOrWhiteSpace(configuration.CurrentValue.CronSchedule))
            throw new ArgumentNullException(nameof(configuration), $"Can't start service. Missing cron schedule. {nameof(configuration.CurrentValue.CronSchedule)}");

        _version = configuration.CurrentValue.Version;
        _cronScheduleString = configuration.CurrentValue.CronSchedule;
        _schedule = CrontabSchedule.Parse(_cronScheduleString);
        _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SynchronizationCronService is stopping");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var appVersion = !string.IsNullOrEmpty(_version) ? _version : "unknown";
        _logger.LogInformation("{applicationName} Version: {applicationVersion}", assembly.GetName().Name, appVersion);
        _logger.LogInformation("Starting service. Next synchronizations is scheduled at: {nextScheduledTime}", _nextRun);

        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await ExecuteSynchronization(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception caught");
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            });
        });

        return Task.CompletedTask;
    }

    public async Task ExecuteSynchronization(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (DateTime.Now > _nextRun)
            {
                _logger.LogInformation("Synchronization started");
                await _syncHandler.ExecuteSyncronization(cancellationToken);
                _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                _logger.LogInformation("Next synchronization is scheduled at: {nextScheduledTime}", _nextRun);
            }

            await Task.Delay(_taskDelay, cancellationToken);

            CheckCronUpdateSchedule();
        }
    }

    private void CheckCronUpdateSchedule()
    {
        if (_configuration.CurrentValue.CronSchedule != _cronScheduleString)
        {
            _cronScheduleString = _configuration.CurrentValue.CronSchedule;
            try
            {
                _schedule = CrontabSchedule.Parse(_cronScheduleString);
                _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                _logger.LogInformation("Schedule changed. Next synchronization is scheduled at: {nextScheduledTime}", _nextRun);
            }
            catch (Exception e)
            {
                _logger.LogError("Invalid cron configuration. Stopping synchronization service");
                _logger.LogError("Error {error}", e.Message);
                throw;
            }
        }
    }
}