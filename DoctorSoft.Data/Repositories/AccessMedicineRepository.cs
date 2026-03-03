using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessMedicineRepository : IMedicineRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessMedicineRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<MedicineInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Medicine], [Type] FROM [Medicine] ORDER BY [Medicine]";

        var rows = new List<MedicineInfo>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return rows;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new MedicineInfo
            {
                Medicine = reader["Medicine"]?.ToString() ?? string.Empty,
                Type = reader["Type"]?.ToString() ?? string.Empty
            });
        }

        return rows;
    }

    public async Task<MedicineInfo?> GetByNameAsync(string medicineName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Medicine], [Type] FROM [Medicine] WHERE [Medicine] = ?";
        var parameter = command.CreateParameter();
        parameter.Value = medicineName.Trim();
        command.Parameters.Add(parameter);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null || !await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MedicineInfo
        {
            Medicine = reader["Medicine"]?.ToString() ?? string.Empty,
            Type = reader["Type"]?.ToString() ?? string.Empty
        };
    }
}
