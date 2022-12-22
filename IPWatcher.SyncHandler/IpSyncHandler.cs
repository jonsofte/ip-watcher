using IPWatcher.Abstractions.Domain;
using IPWatcher.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace IPWatcher.SyncHandler
{
    public class IpSyncHandler : IIpSyncService
    {
        private readonly ILogger<IpSyncHandler> _logger;
        private readonly ICurrentIPResolver _getIPClient;
        private readonly IStorageRepository _storageIP;

        public IpSyncHandler(ILogger<IpSyncHandler> logger, ICurrentIPResolver ip_client, IStorageRepository storage_client)
        {
            _logger = logger;
            _getIPClient = ip_client;
            _storageIP = storage_client;
        }   

        public async Task StartSync(CancellationToken cancellationToken)
        {
            var ipAddressTask = _getIPClient.GetIP(cancellationToken);
            var expectedIPAddressTask = _storageIP.GetLastStoredIP(cancellationToken);
            IPAddress expectedID = new() { Ip = "127.0.0.1"};

            var ipAddress = await ipAddressTask;
            var expectedIPAddress = await expectedIPAddressTask;

            if (!ipAddress.IsSuccess) return;

            if (expectedIPAddress.IsSuccess)
            {
                expectedID = expectedIPAddress.Value;
            }
            if (ipAddress.Value.Ip == expectedID.Ip)
            {
                _logger.LogDebug("IPs are equal: {ip}",ipAddress.Value.Ip);
            }
            else
            {
                _logger.LogInformation("IPs not equal: {current} != {expected}", ipAddress.Value.Ip, expectedID.Ip);
                var updateResult = await _storageIP.UpdateCurrentIPAddress(ipAddress.Value, cancellationToken);
                if (updateResult.IsSuccess)
                {
                    _logger.LogInformation("Storage updated: {updateIP}", ipAddress.Value.Ip);
                }
                else
                {
                    _logger.LogError("Storage updated failed: {updateIP}", updateResult.Error);
                }
            }
        }

        public Task StopSync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SyncHandler is stopping");
            return Task.CompletedTask;
        }
    }
}
