namespace IPWatcher.SyncService;

public class CronScheduleConfiguration
{
    private string _cronSchedule;

    public string CronSchedule
    {
        get { return _cronSchedule; }
        set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("CronSchedule", "The Cron Schedule string is not set");
            _cronSchedule = value;
        }
    }

    public CronScheduleConfiguration()
    {
        _cronSchedule = "";
    }
}
