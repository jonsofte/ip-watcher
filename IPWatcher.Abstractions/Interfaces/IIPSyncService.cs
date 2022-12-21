namespace IPWatcher.Abstractions.Interfaces;

public interface IIPSyncService
{
    Task StartSync(CancellationToken cancellationToken);
}
