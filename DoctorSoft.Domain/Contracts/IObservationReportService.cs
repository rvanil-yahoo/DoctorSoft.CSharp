using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IObservationReportService
{
    Task<IReadOnlyList<ObservationReportRecord>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ObservationReportRecord>> GetByDateAndPatientAsync(DateTime date, string patientName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ObservationReportRecord>> GetByPatientAsync(string patientName, CancellationToken cancellationToken = default);
}