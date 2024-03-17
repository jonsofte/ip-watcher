using CSharpFunctionalExtensions;
using IPWatcher.Core.Domain;

namespace IPWatcher.Core.Interfaces;

public interface IStorageRepository
{
    Task<Result<IPAddress>> GetLastStoredIP(CancellationToken cancellationToken);
    Task<Result> UpdateCurrentIPAddress(IPAddress ipAddress, CancellationToken cancellationToken);
}