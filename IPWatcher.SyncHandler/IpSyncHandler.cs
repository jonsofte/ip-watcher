using IPWatcher.Abstractions.Domain;
using IPWatcher.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace IPWatcher.SyncHandler
{
    public class IpSyncHandler : IIpSyncService
    {
        private readonly ILogger<IpSyncHandler> _logger;
        private readonly ICurrentIPResolver _getIPClient;
        private readonly IStorageRepository _storageIP;

        public IpSyncHandler(ILogger<IpSyncHandler> logger, ICurrentIPResolver ip_client, IStorageRepository storage_client, TracerProvider tracerProvider)
        {
            _logger = logger;
            _getIPClient = ip_client;
            _storageIP = storage_client;
        }   

        public async Task StartSync(CancellationToken cancellationToken)
        {
            var activitySource = new ActivitySource("Sample.DistributedTracing");
            var synchronizationActivity = activitySource.StartActivity("Synchronization", ActivityKind.Server);

            var getExpectedIPActivity = activitySource.StartActivity("Get Expected IP Address", ActivityKind.Client, synchronizationActivity!.Context);
            var expectedIPAddressTask = _storageIP.GetLastStoredIP(cancellationToken);

            var getCurrentIPActivity = activitySource.StartActivity("Get IP Address", ActivityKind.Client, synchronizationActivity!.Context);
            var ipAddressTask = _getIPClient.GetIP(cancellationToken);
            var expectedIP = new IPAddress() { Ip = "127.0.0.1"};

            var ipAddress = await ipAddressTask;
            getCurrentIPActivity!.Stop();

            var expectedIPAddress = await expectedIPAddressTask;
            getExpectedIPActivity!.Stop();

            if (!ipAddress.IsSuccess) return;
            _logger.LogInformation("Current IP address: {ip}", ipAddress.Value.Ip);

            if (expectedIPAddress.IsSuccess)
            {
                expectedIP = expectedIPAddress.Value;
                _logger.LogInformation("Fetched expected IP: {ip}", expectedIP.Ip);
            }
            if (ipAddress.Value.Ip == expectedIP.Ip)
            {
                _logger.LogInformation("IPs are equal. Doing nothing",ipAddress.Value.Ip);
            }
            else
            {
                _logger.LogInformation("IPs not equal: {current} != {expected}", ipAddress.Value.Ip, expectedIP.Ip);
                var updateActivity = activitySource.StartActivity("Update Current IP Address", ActivityKind.Client, synchronizationActivity!.Context);
                var updateResult = await _storageIP.UpdateCurrentIPAddress(ipAddress.Value, cancellationToken);
                updateActivity!.Stop();
                if (updateResult.IsSuccess)
                {
                    _logger.LogInformation("Storage updated: {updateIP}", ipAddress.Value.Ip);
                }
                else
                {
                    _logger.LogError("Storage updated failed: {updateIP}", updateResult.Error);
                }
            }
            synchronizationActivity!.Stop();
        }

        public Task StopSync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SyncHandler is stopping");
            return Task.CompletedTask;
        }
    }
}
