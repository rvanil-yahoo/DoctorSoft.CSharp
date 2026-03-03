using DoctorSoft.App.Configuration;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;
using Serilog;

namespace DoctorSoft.App.Security;

public sealed class ResilientUserCredentialStore : IUserCredentialStore
{
    private readonly IUserCredentialStore primaryStore;
    private readonly bool primaryEnabled;
    private readonly bool fallbackEnabled;
    private readonly Dictionary<string, UserAccount> fallbackUsers;

    public ResilientUserCredentialStore(IUserCredentialStore primaryStore, AuthenticationOptions options)
    {
        this.primaryStore = primaryStore;
        primaryEnabled = options.EnableDatabasePrimary;
        fallbackEnabled = options.EnableDevFallback;
        fallbackUsers = options.DevUsers
            .Where(u => !string.IsNullOrWhiteSpace(u.UserName))
            .ToDictionary(
                keySelector: u => u.UserName.Trim(),
                elementSelector: u => new UserAccount
                {
                    UserName = u.UserName.Trim(),
                    EncodedPassword = EncodeLegacy(u.UserName.Trim(), u.Password)
                },
                comparer: StringComparer.OrdinalIgnoreCase);
    }

    public async Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (!primaryEnabled)
        {
            fallbackUsers.TryGetValue(userName.Trim(), out var localUser);
            return localUser;
        }

        try
        {
            return await primaryStore.FindByUserNameAsync(userName, cancellationToken);
        }
        catch (PlatformNotSupportedException) when (fallbackEnabled)
        {
            Log.Warning("Primary credential store is unavailable on this runtime. Using configured fallback users.");
            fallbackUsers.TryGetValue(userName.Trim(), out var user);
            return user;
        }
        catch (Exception exception) when (fallbackEnabled)
        {
            Log.Warning(exception, "Primary credential store failed. Using configured fallback users.");
            fallbackUsers.TryGetValue(userName.Trim(), out var user);
            return user;
        }
    }

    private static string EncodeLegacy(string userName, string password)
    {
        var plain = $"{userName},{password};";
        return string.Concat(plain.Select(ch => $"${(int)ch / 7.0}$"));
    }
}
