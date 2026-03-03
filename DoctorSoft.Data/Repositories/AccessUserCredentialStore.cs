using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;
using DoctorSoft.Data.Access;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessUserCredentialStore : IUserCredentialStore
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessUserCredentialStore(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT uname, [password] FROM un WHERE uname = ?";
        var userNameParameter = command.CreateParameter();
        userNameParameter.Value = userName;
        command.Parameters.Add(userNameParameter);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null || !await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new UserAccount
        {
            UserName = reader["uname"].ToString() ?? string.Empty,
            EncodedPassword = reader["password"].ToString() ?? string.Empty
        };
    }
}
