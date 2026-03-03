using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPatientHistoryReportService
{
    Task<IReadOnlyList<PatientHistoryReportRecord>> GetByPatientAsync(string patientName, CancellationToken cancellationToken = default);
}