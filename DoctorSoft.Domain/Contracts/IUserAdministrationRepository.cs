using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IUserAdministrationRepository
{
    Task<IReadOnlyList<UserAdminRecord>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task AddUserAsync(string userName, string plainPassword, CancellationToken cancellationToken = default);
    Task UpdatePasswordAsync(string userName, string plainPassword, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string userName, CancellationToken cancellationToken = default);
}
