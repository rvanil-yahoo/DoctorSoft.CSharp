namespace DoctorSoft.App.Configuration;

public sealed class AppSettings
{
    public string LogDirectory { get; set; } = "logs";
    public string BackupDirectory { get; set; } = "backups";
    public int MaintenanceHistoryFileScanLimit { get; set; } = 15;
    public int MaintenanceHistoryDefaultMaxRows { get; set; } = 500;
}
