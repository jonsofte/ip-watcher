namespace IPWatcher.Abstractions.Domain;

public class ChangeEntry
{
    public DateTimeOffset Time { get; set; }
    public required IPAddress Ip { get; set; }
}