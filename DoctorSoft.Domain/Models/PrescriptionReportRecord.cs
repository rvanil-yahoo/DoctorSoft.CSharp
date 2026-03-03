namespace DoctorSoft.Domain.Models;

public sealed class PrescriptionReportRecord
{
    public int PrescId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientAddress { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public DateTime Date { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Medicine { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicAddr { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
}
