namespace DoctorSoft.Domain.Models;

public sealed class Patient
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
}
