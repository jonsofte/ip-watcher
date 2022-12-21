using CSharpFunctionalExtensions;

namespace IPWatcher.Abstractions.Interfaces;
public interface ICurrentIPResolver
{
    Task<Result<IPAddress>> GetIP(CancellationToken cancellationToken);
}
