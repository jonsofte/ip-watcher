namespace IPWatcher.SyncService;

public class ApplicationConfiguration
{
    private string _cronSchedule;
    private string _version;

    public string CronSchedule
    {
        get { return _cronSchedule; }
        set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("CronSchedule", "The Cron Schedule string is not set");
            _cronSchedule = value;
        }
    }

    public string Version 
    { 
        get { return _version; }  
        set
        {
            if (string.IsNullOrWhiteSpace(value)) _version = "";
            _version = value;
        }
    }

    public ApplicationConfiguration()
    {
        _cronSchedule = "";
        _version= "";
    }
}
