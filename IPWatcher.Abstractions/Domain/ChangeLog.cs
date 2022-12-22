namespace IPWatcher.Abstractions.Domain;

public class ChangeLog
{
    public List<ChangeEntry> Log { get; set; } = new List<ChangeEntry>();
}
