namespace IpWatcher.Tests;

using Microsoft.Extensions.Logging;

public class LoggerAggregate<T> : ILogger<T>
{
    private readonly List<ILogger<T>> _loggers = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;
    public bool IsEnabled(LogLevel logLevel) => _loggers.Any(logger => logger.IsEnabled(logLevel));
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (var logger in _loggers)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    public void AddLogger(ILogger<T> logger)
    {
        _loggers.Add(logger);
    }
}