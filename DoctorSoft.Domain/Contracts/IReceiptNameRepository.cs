using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IReceiptNameRepository
{
    Task<IReadOnlyList<ReceiptName>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(string name, bool requiresPatientSelection, CancellationToken cancellationToken = default);
    Task UpdateAsync(string oldName, string newName, bool requiresPatientSelection, CancellationToken cancellationToken = default);
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
    Task<ReceiptName?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}