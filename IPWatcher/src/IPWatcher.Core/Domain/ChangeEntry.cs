namespace IPWatcher.Core.Domain;

public record ChangeEntry
{
    public required DateTimeOffset Time { get; set; }
    public required IPAddress Ip { get; set; }
}