using System.Diagnostics;
using Microsoft.Extensions.Logging;

using CSharpFunctionalExtensions;
using NSubstitute;
using Xunit.Abstractions;

using IPWatcher.SyncHandler;
using IPWatcher.Core.Interfaces;
using IPWatcher.Core.Domain;

namespace IpWatcher.Tests;

public class IPWatcherIntegrationTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task When_ips_are_identical_do_nothing()
    {
        // Setup logger
        var loggerAggregate = new LoggerAggregate<IpSyncHandler>();
        var logger = Substitute.For<ILogger<IpSyncHandler>>();
        loggerAggregate.AddLogger(_output.BuildLoggerFor<IpSyncHandler>(LogLevel.Debug));
        loggerAggregate.AddLogger(logger);

        // Mock external dependencies
        ICurrentIPAddressResolver ipClient = Substitute.For<ICurrentIPAddressResolver>();
        IStorageRepository repository = Substitute.For<IStorageRepository>();
        ActivitySource activitySource = CreateMockActivitySourceAndListener();

        ipClient.GetIP(Arg.Any<CancellationToken>())
           .Returns(Task.FromResult(Result.Success(new IPAddress() { Ip = "192.168.0.1" })));

        repository.GetLastStoredIP(Arg.Any<CancellationToken>())
           .Returns(Task.FromResult(Result.Success(new IPAddress() { Ip = "192.168.0.1" })));

        // Initialize and call comparer
        var syncHandler = new IpSyncHandler(ipClient, repository, activitySource, loggerAggregate);
        await syncHandler.ExecuteSyncronization(CancellationToken.None);

        // Assert results
        await ipClient.Received(1).GetIP(Arg.Any<CancellationToken>());
        await repository.Received(1).GetLastStoredIP(Arg.Any<CancellationToken>());
        await repository.DidNotReceive().UpdateCurrentIPAddress(Arg.Any<IPAddress>(), Arg.Any<CancellationToken>());

        logger.DidNotReceive().AnyLogOfType(LogLevel.Error);
        logger.DidNotReceive().AnyLogOfType(LogLevel.Warning);
        logger.Received(1).LogInformationMessageContains("IPs are equal");
    }

    [Fact]
    public async Task When_ips_are_different_update_storage_whith_new_value()
    {
        // Setup logger
        var loggerAggregate = new LoggerAggregate<IpSyncHandler>();
        var logger = Substitute.For<ILogger<IpSyncHandler>>();
        loggerAggregate.AddLogger(_output.BuildLoggerFor<IpSyncHandler>(LogLevel.Debug));
        loggerAggregate.AddLogger(logger);

        // Mock external dependencies
        ICurrentIPAddressResolver ipClient = Substitute.For<ICurrentIPAddressResolver>();
        IStorageRepository repository = Substitute.For<IStorageRepository>();
        ActivitySource source = CreateMockActivitySourceAndListener();

        ipClient.GetIP(Arg.Any<CancellationToken>())
           .Returns(Task.FromResult(Result.Success(new IPAddress() { Ip = "192.168.0.1" })));

        repository.GetLastStoredIP(Arg.Any<CancellationToken>())
           .Returns(Task.FromResult(Result.Success(new IPAddress() { Ip = "172.16.0.1" })));

        // Initialize and call comparer
        var syncHandler = new IpSyncHandler(ipClient, repository, source, loggerAggregate);
        await syncHandler.ExecuteSyncronization(CancellationToken.None);

        // Assert results
        await ipClient.Received(1).GetIP(Arg.Any<CancellationToken>());
        await repository.Received(1).GetLastStoredIP(Arg.Any<CancellationToken>());
        await repository.Received(1).UpdateCurrentIPAddress(new IPAddress() { Ip = "192.168.0.1" }, Arg.Any<CancellationToken>());

        logger.DidNotReceive().AnyLogOfType(LogLevel.Error);
        logger.DidNotReceive().AnyLogOfType(LogLevel.Warning);
        logger.Received(1).LogInformationMessageContains("IPs are not equal");
        logger.Received(1).LogInformationMessageContains("Storage updated");
    }

    private static ActivitySource CreateMockActivitySourceAndListener()
    {
        var activitySource = new ActivitySource("Test source");
        var activityListener = new ActivityListener
        {
            ShouldListenTo = s => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);

        return activitySource;
    }
}