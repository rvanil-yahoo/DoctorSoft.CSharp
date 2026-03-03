namespace DoctorSoft.Domain.Models;

public sealed class AppointmentReportRecord
{
    public DateTime StartDate { get; set; }
    public string AppTime { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string EventDetails { get; set; } = string.Empty;
    public bool Status { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicAddr { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
}
