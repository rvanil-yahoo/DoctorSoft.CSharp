namespace DoctorSoft.Domain.Models;

public sealed class PrescriptionLine
{
    public int PrescId { get; set; }
    public string Medicine { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
}
