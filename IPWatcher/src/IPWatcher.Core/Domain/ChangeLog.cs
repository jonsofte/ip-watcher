namespace IPWatcher.Core.Domain;

public record ChangeLog
{
    public List<ChangeEntry> Log { get; set; } = [];
}
