namespace IPWatcher.Core.Domain;

public record IPAddress
{
    public required string Ip { get; set; }
}