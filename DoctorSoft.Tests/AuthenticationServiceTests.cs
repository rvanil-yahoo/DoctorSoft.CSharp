using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;
using DoctorSoft.Domain.Services;

namespace DoctorSoft.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task SignInAsync_Fails_WhenUserNameMissing()
    {
        var service = BuildService(new UserAccount { UserName = "admin", EncodedPassword = EncodeLegacy("admin,pass;") });

        var result = await service.SignInAsync("   ", "pass");

        Assert.False(result.Success);
        Assert.Equal("Username is required.", result.Message);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenPasswordMissing()
    {
        var service = BuildService(new UserAccount { UserName = "admin", EncodedPassword = EncodeLegacy("admin,pass;") });

        var result = await service.SignInAsync("admin", "  ");

        Assert.False(result.Success);
        Assert.Equal("Password is required.", result.Message);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenUserNotFound()
    {
        var service = BuildService(new UserAccount { UserName = "admin", EncodedPassword = EncodeLegacy("admin,pass;") });

        var result = await service.SignInAsync("other", "pass");

        Assert.False(result.Success);
        Assert.Equal("Username does not exist.", result.Message);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenStoredCredentialFormatInvalid()
    {
        var service = new AuthenticationService(
            new InMemoryUserStore(new UserAccount { UserName = "admin", EncodedPassword = "not-legacy" }),
            new PassthroughDecoder());

        var result = await service.SignInAsync("admin", "pass");

        Assert.False(result.Success);
        Assert.Equal("Stored credentials are invalid.", result.Message);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenPasswordDoesNotMatch()
    {
        var service = BuildService(new UserAccount { UserName = "admin", EncodedPassword = EncodeLegacy("admin,pass;") });

        var result = await service.SignInAsync("admin", "wrong");

        Assert.False(result.Success);
        Assert.Equal("Invalid password.", result.Message);
    }

    [Fact]
    public async Task SignInAsync_Passes_WhenPasswordMatchesCaseInsensitive()
    {
        var service = BuildService(new UserAccount { UserName = "admin", EncodedPassword = EncodeLegacy("admin,Pass;") });

        var result = await service.SignInAsync("admin", "pass");

        Assert.True(result.Success);
        Assert.Equal("Login successful.", result.Message);
    }

    [Fact]
    public void AuthenticationResult_ConvenienceMethods_ReturnExpectedFlagsAndMessages()
    {
        var success = DoctorSoft.Domain.AuthenticationResult.Passed();
        var failure = DoctorSoft.Domain.AuthenticationResult.Failed("x");

        Assert.True(success.Success);
        Assert.Equal("Login successful.", success.Message);
        Assert.False(failure.Success);
        Assert.Equal("x", failure.Message);
    }

    private static AuthenticationService BuildService(UserAccount account)
    {
        return new AuthenticationService(new InMemoryUserStore(account), new LegacyDecoder());
    }

    private static string EncodeLegacy(string plainText)
    {
        var segments = plainText.Select(ch => $"${(int)ch / 7.0}$");
        return string.Concat(segments);
    }

    private sealed class InMemoryUserStore : IUserCredentialStore
    {
        private readonly UserAccount userAccount;

        public InMemoryUserStore(UserAccount userAccount)
        {
            this.userAccount = userAccount;
        }

        public Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                string.Equals(userAccount.UserName, userName, StringComparison.OrdinalIgnoreCase)
                    ? userAccount
                    : null);
        }
    }

    private sealed class LegacyDecoder : ILegacyPasswordDecoder
    {
        public string Decode(string encoded)
        {
            var output = new List<char>();
            var start = 0;

            while (start < encoded.Length)
            {
                var open = encoded.IndexOf('$', start);
                if (open < 0)
                {
                    break;
                }

                var close = encoded.IndexOf('$', open + 1);
                if (close < 0)
                {
                    break;
                }

                var token = encoded.Substring(open + 1, close - open - 1);
                if (double.TryParse(token, out var value))
                {
                    output.Add((char)Math.Round(value * 7));
                }

                start = close + 1;
            }

            return new string(output.ToArray());
        }
    }

    private sealed class PassthroughDecoder : ILegacyPasswordDecoder
    {
        public string Decode(string encoded) => encoded;
    }
}
