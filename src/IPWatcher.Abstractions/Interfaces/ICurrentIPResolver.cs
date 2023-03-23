using CSharpFunctionalExtensions;
using IPWatcher.Abstractions.Domain;

namespace IPWatcher.Abstractions.Interfaces;
public interface ICurrentIPResolver
{
    Task<Result<IPAddress>> GetIP(CancellationToken cancellationToken);
}
