using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPrescriptionRepository
{
    Task<int> GetNextPrescriptionIdAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsForPatientAndDateAsync(string patientName, DateTime date, CancellationToken cancellationToken = default);
    Task SaveAsync(Prescription prescription, CancellationToken cancellationToken = default);
}
