namespace DoctorSoft.Domain.Models;

public sealed class PatientHistoryReportRecord
{
    public string PatientName { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string TestDescription { get; set; } = string.Empty;
    public string Observations { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicAddr { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
}