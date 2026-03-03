namespace DoctorSoft.Domain.Models;

public sealed class UserAccount
{
    public string UserName { get; init; } = string.Empty;
    public string EncodedPassword { get; init; } = string.Empty;
}
