namespace DoctorSoft.App.Configuration;

public sealed class AuthenticationOptions
{
    public bool EnableDatabasePrimary { get; set; } = true;
    public bool EnableDevFallback { get; set; }
    public List<DevUserOptions> DevUsers { get; set; } = new();
}

public sealed class DevUserOptions
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
