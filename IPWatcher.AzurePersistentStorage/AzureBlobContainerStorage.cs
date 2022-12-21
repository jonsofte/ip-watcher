using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.Storage.Blobs;
using CSharpFunctionalExtensions;
using IPWatcher.Abstractions.Interfaces;
using IPWatcher.Abstractions;
using IPWatcher.AzurePersistantStorage;
using Microsoft.Extensions.Options;

namespace IPStorage
{
    public class AzureBlobContainerStorage : IStorageRepository
    {
        private readonly ILogger<AzureBlobContainerStorage> _logger;
        private readonly BlobServiceClient _client;
        private readonly AzureStorageConfiguration _configuration;

        public AzureBlobContainerStorage(ILogger<AzureBlobContainerStorage> logger, IOptions<AzureStorageConfiguration> configuration) 
        {
            _logger = logger;
            _configuration = configuration.Value;

            var certificate = new X509Certificate2(
                _configuration.Authentication.X509CertificatePath, 
                _configuration.Authentication.X509Password);

            var clientCertificateCredential = new ClientCertificateCredential(
                tenantId: _configuration.Authentication.AzureADTenantID,
                clientId: _configuration.Authentication.AzureADClientID,
                certificate
                );

            _client = new BlobServiceClient(new Uri(_configuration.Blob.AccountUri), clientCertificateCredential);
        }

        private async Task<Result<ChangeLog>> GetChangeLog(CancellationToken cancellationToken)
        {
            var containerClient = _client.GetBlobContainerClient(_configuration.Blob.ContainerName);
            var blobClient = containerClient.GetBlobClient(_configuration.Blob.ChangeLogFile);

            try
            {
                var fileExists = (await blobClient.ExistsAsync(cancellationToken)).Value;
                if (!fileExists) return Result.Success<ChangeLog>(new ChangeLog());

                BinaryData downloadedData = (await blobClient.DownloadContentAsync(cancellationToken)).Value.Content;
                var changeLog = JsonSerializer.Deserialize<ChangeLog>(downloadedData);
                _logger.LogInformation("Fetched changelog from storage");
                return Result.Success(changeLog!);
            }
            catch (Exception ex)
            {
                return Result.Failure<ChangeLog>($"Error {ex.Message}");
            }
        }

        public async Task<Result<IPAddress>> GetLastStoredIP(CancellationToken cancellationToken)
        {
            var containerClient = _client.GetBlobContainerClient(_configuration.Blob.ContainerName);
            var blobClient = containerClient.GetBlobClient(_configuration.Blob.CurrentIPFile);

            try 
            { 
                BinaryData downloadedData = (await blobClient.DownloadContentAsync(cancellationToken)).Value.Content;
                var ip = JsonSerializer.Deserialize<IPAddress>(downloadedData);
                _logger.LogInformation("Fetched {ip} from storage", ip!.Ip);
                return Result.Success(ip);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed getting ip from storage: {message}", ex.Message);
                _logger.LogError("{expetion}", ex.StackTrace);
                return Result.Failure<IPAddress>($"Error {ex.Message}");
            }
        }

        public async Task<Result> UpdateCurrentIPAddress(IPAddress ipAddress, CancellationToken cancellationToken)
        {
            var updateResult = await UpdateLastIPFile(ipAddress, cancellationToken);
            if (updateResult.IsFailure) return updateResult;

            var logUpdateResult = await UpdateLog(ipAddress, cancellationToken);
            return logUpdateResult;
        }

        private async Task<Result> UpdateLog(IPAddress ipAddress, CancellationToken cancellationToken)
        {
            Result<ChangeLog> changeLogResult = await GetChangeLog(cancellationToken);

            if (changeLogResult.IsFailure)
            {
                return changeLogResult;
            }

            var changeLog = changeLogResult.Value;
            changeLog.Log.Add(new ChangeEntry() { Ip = ipAddress, Time = DateTimeOffset.Now });

            Result updateChangeLogResult = await UpdateChangeLog(changeLog, cancellationToken);
            return updateChangeLogResult;
        }

        private async Task<Result> UpdateChangeLog(ChangeLog changeLog, CancellationToken cancellationToken)
        {
            try
            {
                var containerClient = _client.GetBlobContainerClient(_configuration.Blob.ContainerName);
                var blobClient = containerClient.GetBlobClient(_configuration.Blob.ChangeLogFile);
                string changelogSerialized = JsonSerializer.Serialize(changeLog);
                await blobClient.UploadAsync(BinaryData.FromString(changelogSerialized), overwrite: true, cancellationToken);
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error {ex.Message}");
            }

            return Result.Success();
        }

        private async Task<Result> UpdateLastIPFile(IPAddress ipAddress, CancellationToken cancellationToken)
        {
            try
            {
                var containerClient = _client.GetBlobContainerClient(_configuration.Blob.ContainerName);
                var blobClient = containerClient.GetBlobClient(_configuration.Blob.CurrentIPFile);
                string ipSerialized = JsonSerializer.Serialize(ipAddress);
                await blobClient.UploadAsync(BinaryData.FromString(ipSerialized), overwrite: true, cancellationToken);
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error {ex.Message}");
            }
            return Result.Success();
        }
    }
}
