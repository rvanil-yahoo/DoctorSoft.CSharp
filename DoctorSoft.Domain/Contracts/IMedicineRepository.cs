using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IMedicineRepository
{
    Task<IReadOnlyList<MedicineInfo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MedicineInfo?> GetByNameAsync(string medicineName, CancellationToken cancellationToken = default);
}
