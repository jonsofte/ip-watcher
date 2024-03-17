using Microsoft.Extensions.Logging;
using NSubstitute;

namespace IpWatcher.Tests;

public static class LoggerTestExtensions
{
    public static void AnyLogOfType<T>(this ILogger<T> logger, LogLevel level) where T : class
    {
        logger.Log(
            level,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
            );
    }

    public static void LogInformationMessageContains<T>(this ILogger<T> logger, string message) where T : class
    {
        logger.Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains(message)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
            );
    }
}
