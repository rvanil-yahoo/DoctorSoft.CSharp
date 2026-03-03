namespace DoctorSoft.Domain.Models;

public sealed class ReceiptName
{
    public string Name { get; set; } = string.Empty;
    public bool RequiresPatientSelection { get; set; }
}