using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPatientHistoryRepository
{
    Task<IReadOnlyList<PatientHistoryEntry>> SearchAsync(string? patientNameFilter, DateTime? testDateFilter, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string patientName, DateTime testDate, CancellationToken cancellationToken = default);
    Task AddAsync(PatientHistoryEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(PatientHistoryEntry entry, string originalPatientName, DateTime originalTestDate, CancellationToken cancellationToken = default);
    Task DeleteAsync(string patientName, DateTime testDate, CancellationToken cancellationToken = default);
}