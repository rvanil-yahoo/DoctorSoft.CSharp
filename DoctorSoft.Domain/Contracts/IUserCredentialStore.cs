using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IUserCredentialStore
{
    Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
}
