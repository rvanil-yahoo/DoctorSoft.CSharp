using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessPaymentNameRepository : IPaymentNameRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessPaymentNameRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PaymentName>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [payname] FROM [payname] ORDER BY [payname]";

        var list = new List<PaymentName>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PaymentName
            {
                Name = reader["payname"]?.ToString() ?? string.Empty
            });
        }

        return list;
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [payname] WHERE [payname] = ?";
        AddParameter(command, name.Trim());

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value) > 0;
    }

    public async Task AddAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO [payname] ([payname]) VALUES (?)";
        AddParameter(command, name.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RenameAsync(string oldName, string newName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE [payname] SET [payname] = ? WHERE [payname] = ?";
        AddParameter(command, newName.Trim());
        AddParameter(command, oldName.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM [payname] WHERE [payname] = ?";
        AddParameter(command, name.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}