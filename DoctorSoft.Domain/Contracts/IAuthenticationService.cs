namespace DoctorSoft.Domain.Contracts;

public interface IAuthenticationService
{
    Task<AuthenticationResult> SignInAsync(string userName, string password, CancellationToken cancellationToken = default);
}
