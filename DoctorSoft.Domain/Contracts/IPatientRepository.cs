using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPatientRepository
{
    Task<IReadOnlyList<Patient>> SearchAsync(string? nameFilter, string? addressFilter, string? bloodGroupFilter, CancellationToken cancellationToken = default);
    Task<Patient?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, string originalName, CancellationToken cancellationToken = default);
}
