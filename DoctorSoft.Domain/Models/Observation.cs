namespace DoctorSoft.Domain.Models;

public sealed class Observation
{
    public DateTime Date { get; set; }
    public string Time { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string ObservationText { get; set; } = string.Empty;
    public string TestsRecommended { get; set; } = string.Empty;
}