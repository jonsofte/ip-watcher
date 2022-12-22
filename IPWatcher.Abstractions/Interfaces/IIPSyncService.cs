namespace IPWatcher.Abstractions.Interfaces;

public interface IIpSyncService
{
    Task StartSync(CancellationToken cancellationToken);
    Task StopSync(CancellationToken cancellationToken);
}
