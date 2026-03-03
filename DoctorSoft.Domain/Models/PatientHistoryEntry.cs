namespace DoctorSoft.Domain.Models;

public sealed class PatientHistoryEntry
{
    public string PatientName { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string TestDescription { get; set; } = string.Empty;
    public string Observations { get; set; } = string.Empty;
}