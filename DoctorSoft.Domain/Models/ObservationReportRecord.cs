namespace DoctorSoft.Domain.Models;

public sealed class ObservationReportRecord
{
    public DateTime Date { get; set; }
    public string Time { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string ObservationText { get; set; } = string.Empty;
    public string TestsRecommended { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicAddr { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
}