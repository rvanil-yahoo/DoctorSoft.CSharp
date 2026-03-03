using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessUserAdministrationRepository : IUserAdministrationRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessUserAdministrationRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<UserAdminRecord>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [uname] FROM [un] ORDER BY [uname]";

        var rows = new List<UserAdminRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return rows;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new UserAdminRecord
            {
                UserName = reader["uname"]?.ToString() ?? string.Empty
            });
        }

        return rows;
    }

    public async Task AddUserAsync(string userName, string plainPassword, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO [un] ([uname], [password]) VALUES (?, ?)";
        AddParameter(command, userName.Trim());
        AddParameter(command, EncodeLegacy(userName.Trim(), plainPassword));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdatePasswordAsync(string userName, string plainPassword, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE [un] SET [password] = ? WHERE [uname] = ?";
        AddParameter(command, EncodeLegacy(userName.Trim(), plainPassword));
        AddParameter(command, userName.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(string userName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM [un] WHERE [uname] = ?";
        AddParameter(command, userName.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string EncodeLegacy(string userName, string password)
    {
        var plain = $"{userName},{password};";
        return string.Concat(plain.Select(ch => $"${(int)ch / 7.0}$"));
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
