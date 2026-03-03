using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IObservationRepository
{
    Task<IReadOnlyList<Observation>> SearchAsync(string? patientNameFilter, DateTime? dateFilter, CancellationToken cancellationToken = default);
    Task<Observation?> BuildDraftForPatientAsync(string patientName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string patientName, DateTime date, CancellationToken cancellationToken = default);
    Task AddAsync(Observation observation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Observation observation, string originalPatientName, DateTime originalDate, CancellationToken cancellationToken = default);
    Task DeleteAsync(string patientName, DateTime date, CancellationToken cancellationToken = default);
}