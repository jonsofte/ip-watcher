using CSharpFunctionalExtensions;
using IPWatcher.Abstractions.Domain;

namespace IPWatcher.Abstractions.Interfaces;

public interface IStorageRepository
{
    Task<Result<IPAddress>> GetLastStoredIP(CancellationToken cancellationToken);
    Task<Result> UpdateCurrentIPAddress(IPAddress ipAddress, CancellationToken cancellationToken);
}