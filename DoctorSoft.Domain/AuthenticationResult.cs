namespace DoctorSoft.Domain;

public sealed record AuthenticationResult(bool Success, string Message)
{
    public static AuthenticationResult Passed() => new(true, "Login successful.");
    public static AuthenticationResult Failed(string message) => new(false, message);
}
