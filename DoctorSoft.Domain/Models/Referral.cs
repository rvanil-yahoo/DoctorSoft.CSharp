namespace DoctorSoft.Domain.Models;

public sealed class Referral
{
    public DateTime RefDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientAddress { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public string PatientSex { get; set; } = string.Empty;
    public string FromDoctor { get; set; } = string.Empty;
    public string FromClinic { get; set; } = string.Empty;
    public string FromClinicAddress { get; set; } = string.Empty;
    public string ToDoctor { get; set; } = string.Empty;
    public string ToClinic { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}