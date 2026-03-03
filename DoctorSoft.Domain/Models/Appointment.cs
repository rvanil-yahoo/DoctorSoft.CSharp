namespace DoctorSoft.Domain.Models;

public sealed class Appointment
{
    public DateTime StartDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientAddress { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public string PatientSex { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string EventDetails { get; set; } = string.Empty;
    public string AppTime { get; set; } = string.Empty;
    public bool Status { get; set; }
    public DateTime? DateAdded { get; set; }
}
