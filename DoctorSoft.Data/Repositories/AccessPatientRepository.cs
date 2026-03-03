using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessPatientRepository : IPatientRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessPatientRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Patient>> SearchAsync(
        string? nameFilter,
        string? addressFilter,
        string? bloodGroupFilter,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT [Patient_Name], [Patient_address], [Patient_Phone], [Patient_age], [Patient_sex], [bg] " +
            "FROM [patient] WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            command.CommandText += " AND [Patient_Name] LIKE ?";
            AddParameter(command, $"*{nameFilter.Trim()}*");
        }

        if (!string.IsNullOrWhiteSpace(addressFilter))
        {
            command.CommandText += " AND [Patient_address] LIKE ?";
            AddParameter(command, $"*{addressFilter.Trim()}*");
        }

        if (!string.IsNullOrWhiteSpace(bloodGroupFilter))
        {
            command.CommandText += " AND [bg] = ?";
            AddParameter(command, bloodGroupFilter.Trim());
        }

        command.CommandText += " ORDER BY [Patient_Name]";

        var patients = new List<Patient>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return patients;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            patients.Add(MapPatient(reader));
        }

        return patients;
    }

    public async Task<Patient?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT [Patient_Name], [Patient_address], [Patient_Phone], [Patient_age], [Patient_sex], [bg] " +
            "FROM [patient] WHERE [Patient_Name] = ?";
        AddParameter(command, name.Trim());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null || !await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapPatient(reader);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO [patient] ([Patient_Name], [Patient_address], [Patient_Phone], [Patient_age], [Patient_sex], [bg]) " +
            "VALUES (?, ?, ?, ?, ?, ?)";

        AddParameter(command, patient.Name.Trim());
        AddParameter(command, patient.Address.Trim());
        AddParameter(command, patient.Phone.Trim());
        AddParameter(command, patient.Age.HasValue ? patient.Age.Value : DBNull.Value);
        AddParameter(command, patient.Sex.Trim());
        AddParameter(command, patient.BloodGroup.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, string originalName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE [patient] SET [Patient_Name] = ?, [Patient_address] = ?, [Patient_Phone] = ?, [Patient_age] = ?, [Patient_sex] = ?, [bg] = ? " +
            "WHERE [Patient_Name] = ?";

        AddParameter(command, patient.Name.Trim());
        AddParameter(command, patient.Address.Trim());
        AddParameter(command, patient.Phone.Trim());
        AddParameter(command, patient.Age.HasValue ? patient.Age.Value : DBNull.Value);
        AddParameter(command, patient.Sex.Trim());
        AddParameter(command, patient.BloodGroup.Trim());
        AddParameter(command, originalName.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static Patient MapPatient(System.Data.Common.DbDataReader reader)
    {
        return new Patient
        {
            Name = reader["Patient_Name"]?.ToString() ?? string.Empty,
            Address = reader["Patient_address"]?.ToString() ?? string.Empty,
            Phone = reader["Patient_Phone"]?.ToString() ?? string.Empty,
            Age = TryInt(reader["Patient_age"]),
            Sex = reader["Patient_sex"]?.ToString() ?? string.Empty,
            BloodGroup = reader["bg"]?.ToString() ?? string.Empty
        };
    }

    private static int? TryInt(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        if (int.TryParse(value.ToString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
