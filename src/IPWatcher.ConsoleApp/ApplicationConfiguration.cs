namespace IPWatcher.ConsoleApp;

public class ApplicationConfiguration
{
    private string _cronSchedule = String.Empty;
    private string _version = String.Empty;
    private string _oTELExporterEndpoint = String.Empty;

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
    public string OTELExporterEndpoint
    {
        get { return _oTELExporterEndpoint; }
        set
        {
            if (string.IsNullOrWhiteSpace(value)) _oTELExporterEndpoint = "";
            _oTELExporterEndpoint = value;
        }
    }
    
}
