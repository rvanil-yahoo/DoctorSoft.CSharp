using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPrescriptionReportService
{
    Task<IReadOnlyList<PrescriptionReportRecord>> GetByPrescriptionIdAsync(int prescId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrescriptionReportRecord>> GetByPatientAsync(string patientName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrescriptionReportRecord>> GetByDateAsync(DateTime date, string? patientName = null, CancellationToken cancellationToken = default);
}
