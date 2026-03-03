namespace DoctorSoft.Domain.Models;

public sealed class Prescription
{
    public int PrescId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientAddress { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public DateTime Date { get; set; }
    public string Time { get; set; } = string.Empty;
    public IReadOnlyList<PrescriptionLine> Lines { get; set; } = Array.Empty<PrescriptionLine>();
}
