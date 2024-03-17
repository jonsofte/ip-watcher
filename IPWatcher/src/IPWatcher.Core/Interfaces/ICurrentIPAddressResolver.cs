using CSharpFunctionalExtensions;
using IPWatcher.Core.Domain;

namespace IPWatcher.Core.Interfaces;

public interface ICurrentIPAddressResolver
{
    Task<Result<IPAddress>> GetIP(CancellationToken cancellationToken);
}
