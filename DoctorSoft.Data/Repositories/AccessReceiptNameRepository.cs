using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessReceiptNameRepository : IReceiptNameRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessReceiptNameRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ReceiptName>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [RecName], [RecPat] FROM [recname] ORDER BY [RecName]";

        var list = new List<ReceiptName>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ReceiptName
            {
                Name = reader["RecName"]?.ToString() ?? string.Empty,
                RequiresPatientSelection = TryBool(reader["RecPat"])
            });
        }

        return list;
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [recname] WHERE [RecName] = ?";
        AddParameter(command, name.Trim());

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value) > 0;
    }

    public async Task AddAsync(string name, bool requiresPatientSelection, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO [recname] ([RecName], [RecPat]) VALUES (?, ?)";
        AddParameter(command, name.Trim());
        AddParameter(command, requiresPatientSelection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(string oldName, string newName, bool requiresPatientSelection, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE [recname] SET [RecName] = ?, [RecPat] = ? WHERE [RecName] = ?";
        AddParameter(command, newName.Trim());
        AddParameter(command, requiresPatientSelection);
        AddParameter(command, oldName.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM [recname] WHERE [RecName] = ?";
        AddParameter(command, name.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ReceiptName?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [RecName], [RecPat] FROM [recname] WHERE [RecName] = ?";
        AddParameter(command, name.Trim());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null || !await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ReceiptName
        {
            Name = reader["RecName"]?.ToString() ?? string.Empty,
            RequiresPatientSelection = TryBool(reader["RecPat"])
        };
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static bool TryBool(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return false;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        if (bool.TryParse(value.ToString(), out var parsedBool))
        {
            return parsedBool;
        }

        if (int.TryParse(value.ToString(), out var parsedInt))
        {
            return parsedInt != 0;
        }

        return false;
    }
}