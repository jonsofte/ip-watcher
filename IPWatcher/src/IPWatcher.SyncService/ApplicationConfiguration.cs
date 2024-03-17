using System.ComponentModel.DataAnnotations;

namespace IPWatcher.SyncService;

public record ApplicationConfiguration
{
    [Required]
    public required string CronSchedule { get; set; }
    [Required]
    public required string Version { get; set; }
    [Required]
    public required string OTELExporterEndpoint { get; set; }    
}
