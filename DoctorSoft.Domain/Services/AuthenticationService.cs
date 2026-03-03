using DoctorSoft.Domain.Contracts;

namespace DoctorSoft.Domain.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserCredentialStore userCredentialStore;
    private readonly ILegacyPasswordDecoder legacyPasswordDecoder;

    public AuthenticationService(IUserCredentialStore userCredentialStore, ILegacyPasswordDecoder legacyPasswordDecoder)
    {
        this.userCredentialStore = userCredentialStore;
        this.legacyPasswordDecoder = legacyPasswordDecoder;
    }

    public async Task<AuthenticationResult> SignInAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return AuthenticationResult.Failed("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return AuthenticationResult.Failed("Password is required.");
        }

        var account = await userCredentialStore.FindByUserNameAsync(userName.Trim(), cancellationToken);
        if (account is null)
        {
            return AuthenticationResult.Failed("Username does not exist.");
        }

        var decoded = legacyPasswordDecoder.Decode(account.EncodedPassword);
        var splitByComma = decoded.IndexOf(',', StringComparison.Ordinal);
        var splitBySemicolon = decoded.IndexOf(';', StringComparison.Ordinal);
        if (splitByComma < 0 || splitBySemicolon <= splitByComma)
        {
            return AuthenticationResult.Failed("Stored credentials are invalid.");
        }

        var storedPassword = decoded.Substring(splitByComma + 1, splitBySemicolon - splitByComma - 1);
        if (!string.Equals(password, storedPassword, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticationResult.Failed("Invalid password.");
        }

        return AuthenticationResult.Passed();
    }
}
