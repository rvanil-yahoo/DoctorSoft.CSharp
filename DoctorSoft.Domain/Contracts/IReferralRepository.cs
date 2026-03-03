using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IReferralRepository
{
    Task<IReadOnlyList<Referral>> SearchAsync(string? patientNameFilter, string? toDoctorFilter, CancellationToken cancellationToken = default);
    Task<Referral?> BuildDraftForPatientAsync(string patientName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string patientName, DateTime refDate, CancellationToken cancellationToken = default);
    Task AddAsync(Referral referral, CancellationToken cancellationToken = default);
    Task UpdateAsync(Referral referral, string originalPatientName, DateTime originalRefDate, CancellationToken cancellationToken = default);
    Task DeleteAsync(string patientName, DateTime refDate, CancellationToken cancellationToken = default);
}