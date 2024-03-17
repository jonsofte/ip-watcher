using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Identity;
using Azure.Storage.Blobs;
using CSharpFunctionalExtensions;

using IPWatcher.Core.Interfaces;
using IPWatcher.Core.Domain;

namespace IPWatcher.Infrastructure.AzurePersistentStorage;

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
            _configuration.AuthenticationX509CertificatePath,
            _configuration.AuthenticationX509Password);

        var clientCertificateCredential = new ClientCertificateCredential(
            tenantId: _configuration.AuthenticationAzureADTenantID,
            clientId: _configuration.AuthenticationAzureADClientID,
            certificate
            );

        _client = new BlobServiceClient(new Uri(_configuration.BlobAccountUri), clientCertificateCredential);
    }

    private async Task<Result<ChangeLog>> GetChangeLog(CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = _client.GetBlobContainerClient(_configuration.BlobContainerName);
            var blobClient = containerClient.GetBlobClient(_configuration.BlobChangeLogFile);

            if (!(await blobClient.ExistsAsync(cancellationToken)).Value)
            {
                return new ChangeLog();
            }

            BinaryData downloadedData = (await blobClient.DownloadContentAsync(cancellationToken)).Value.Content;
            var changeLog = JsonSerializer.Deserialize<ChangeLog>(downloadedData);
            _logger.LogDebug("Fetched changelog from Azure storage. {logentries} log entries found", changeLog!.Log.Count);
            return changeLog;
        }
        catch (Exception ex)
        {
            return Result.Failure<ChangeLog>($"Error {ex.Message}");
        }
    }

    public async Task<Result<IPAddress>> GetLastStoredIP(CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = _client.GetBlobContainerClient(_configuration.BlobContainerName);
            var blobClient = containerClient.GetBlobClient(_configuration.BlobCurrentIPFile);

            BinaryData downloadedData = (await blobClient.DownloadContentAsync(cancellationToken)).Value.Content;
            var ip = JsonSerializer.Deserialize<IPAddress>(downloadedData);
            _logger.LogDebug("Fetched {ip} from Azure blob", ip!.Ip);
            return ip;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed getting ip from Azure storage: {message}", ex.Message);
            _logger.LogError("{exception}", ex.StackTrace);
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
        var changeLogResult = await GetChangeLog(cancellationToken);

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
            var containerClient = _client.GetBlobContainerClient(_configuration.BlobContainerName);
            var blobClient = containerClient.GetBlobClient(_configuration.BlobChangeLogFile);
            string changelogSerialized = JsonSerializer.Serialize(changeLog);
            await blobClient.UploadAsync(BinaryData.FromString(changelogSerialized), overwrite: true, cancellationToken);
            _logger.LogDebug("Updated Azure ChangeLog. Total {logentries} log entries", changeLog.Log.Count);
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
            var containerClient = _client.GetBlobContainerClient(_configuration.BlobContainerName);
            var blobClient = containerClient.GetBlobClient(_configuration.BlobCurrentIPFile);
            string ipSerialized = JsonSerializer.Serialize(ipAddress);
            await blobClient.UploadAsync(BinaryData.FromString(ipSerialized), overwrite: true, cancellationToken);
            _logger.LogDebug("Updated Azure LastIP with IP: {ip}", ipAddress.Ip);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error {ex.Message}");
        }
        return Result.Success();
    }
}
