using System.ComponentModel.DataAnnotations;

namespace IPWatcher.Infrastructure.AzurePersistentStorage;

public record AzureStorageConfiguration
{
    [Required]
    public required string AuthenticationX509CertificatePath { get; set; }
    public required string AuthenticationX509Password { get; set; }
    public required string AuthenticationAzureADTenantID { get; set; }
    public required string AuthenticationAzureADClientID { get; set; }
    [Required, Url]
    public required string BlobAccountUri { get; set; }
    public required string BlobContainerName { get; set; }
    public required string BlobCurrentIPFile { get; set; }
    public required string BlobChangeLogFile { get; set; }
}
