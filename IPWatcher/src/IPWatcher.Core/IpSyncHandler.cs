using System.Diagnostics;
using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;

using IPWatcher.Core.Interfaces;

namespace IPWatcher.SyncHandler;

public class IpSyncHandler(ICurrentIPAddressResolver ipClient, IStorageRepository ipRespository, ActivitySource source, ILogger<IpSyncHandler> logger)
{
    private readonly ICurrentIPAddressResolver _getIPClient = ipClient;
    private readonly IStorageRepository _ipRepository = ipRespository;
    private readonly ActivitySource _source = source;
    private readonly ILogger<IpSyncHandler> _logger = logger;

    public async Task ExecuteSyncronization(CancellationToken cancellationToken)
    { 
        var synchronizationActivity = _source.StartActivity("Synchronization", ActivityKind.Server);

        var getExpectedIPActivity = _source.StartActivity("Get Expected IP Address", ActivityKind.Client, synchronizationActivity!.Context);
        var getExpectedIPAddressTask = _ipRepository.GetLastStoredIP(cancellationToken);

        var getCurrentIPActivity = _source.StartActivity("Get IP Address", ActivityKind.Client, synchronizationActivity.Context);
        var getIpAddressTask = _getIPClient.GetIP(cancellationToken);
        
        var getIpAddressResult = await getIpAddressTask;
        getCurrentIPActivity?.Stop();
        var getExpectedIPAddressResult = await getExpectedIPAddressTask;
        getExpectedIPActivity?.Stop();

        if (Result.Combine(getIpAddressResult, getExpectedIPAddressResult).IsFailure) return;

        var expectedIP = getExpectedIPAddressResult.Value.Ip;
        var currentIP = getIpAddressResult.Value.Ip;

        if (currentIP == expectedIP)
        {
            _logger.LogInformation("IPs are equal: {ip} Doing nothing",currentIP);
        }
        else
        {
            _logger.LogInformation("IPs are not equal: {current} != {expected}", currentIP, expectedIP);
            var updateActivity = _source.StartActivity("Update Current IP Address", ActivityKind.Client, synchronizationActivity!.Context);
            var updateResult = await _ipRepository.UpdateCurrentIPAddress(getIpAddressResult.Value, cancellationToken);
            updateActivity?.Stop();

            if (updateResult.IsSuccess)
            {
                _logger.LogInformation("Storage updated: {updateIP}", currentIP);
            }
            else
            {
                _logger.LogError("Storage updated failed: {updateIP}", updateResult.Error);
            }
        }
        synchronizationActivity?.Stop();
    }
}
