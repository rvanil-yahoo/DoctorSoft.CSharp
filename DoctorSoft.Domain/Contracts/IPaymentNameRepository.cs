using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPaymentNameRepository
{
    Task<IReadOnlyList<PaymentName>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(string name, CancellationToken cancellationToken = default);
    Task RenameAsync(string oldName, string newName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
}