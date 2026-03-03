using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessObservationRepository : IObservationRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessObservationRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Observation>> SearchAsync(string? patientNameFilter, DateTime? dateFilter, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT [Date], [Time], [Patient_Name], [Age], [Sex], [problem], [Observation], [testsrecom] " +
            "FROM [observations] WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(patientNameFilter))
        {
            command.CommandText += " AND [Patient_Name] LIKE ?";
            AddParameter(command, $"*{patientNameFilter.Trim()}*");
        }

        if (dateFilter.HasValue)
        {
            command.CommandText += " AND [Date] = ?";
            AddParameter(command, dateFilter.Value.Date);
        }

        command.CommandText += " ORDER BY [Date] DESC, [Patient_Name]";

        var list = new List<Observation>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapObservation(reader));
        }

        return list;
    }

    public async Task<Observation?> BuildDraftForPatientAsync(string patientName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Patient_age], [Patient_sex] FROM [patient] WHERE [Patient_Name] = ?";
        AddParameter(command, patientName.Trim());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null || !await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Observation
        {
            Date = DateTime.Today,
            Time = DateTime.Now.ToString("HH:mm:ss"),
            PatientName = patientName.Trim(),
            Age = TryInt(reader["Patient_age"]),
            Sex = reader["Patient_sex"]?.ToString() ?? string.Empty,
            Problem = string.Empty,
            ObservationText = string.Empty,
            TestsRecommended = string.Empty
        };
    }

    public async Task<bool> ExistsAsync(string patientName, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [observations] WHERE [Patient_Name] = ? AND [Date] = ?";
        AddParameter(command, patientName.Trim());
        AddParameter(command, date.Date);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value) > 0;
    }

    public async Task AddAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO [observations] ([Date], [Time], [Patient_Name], [Age], [Sex], [problem], [Observation], [testsrecom]) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

        AddParameter(command, observation.Date.Date);
        AddParameter(command, observation.Time.Trim());
        AddParameter(command, observation.PatientName.Trim());
        AddParameter(command, observation.Age.HasValue ? observation.Age.Value : DBNull.Value);
        AddParameter(command, observation.Sex.Trim());
        AddParameter(command, observation.Problem.Trim());
        AddParameter(command, observation.ObservationText.Trim());
        AddParameter(command, observation.TestsRecommended.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Observation observation, string originalPatientName, DateTime originalDate, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE [observations] SET [Date] = ?, [Time] = ?, [Patient_Name] = ?, [Age] = ?, [Sex] = ?, [problem] = ?, [Observation] = ?, [testsrecom] = ? " +
            "WHERE [Patient_Name] = ? AND [Date] = ?";

        AddParameter(command, observation.Date.Date);
        AddParameter(command, observation.Time.Trim());
        AddParameter(command, observation.PatientName.Trim());
        AddParameter(command, observation.Age.HasValue ? observation.Age.Value : DBNull.Value);
        AddParameter(command, observation.Sex.Trim());
        AddParameter(command, observation.Problem.Trim());
        AddParameter(command, observation.ObservationText.Trim());
        AddParameter(command, observation.TestsRecommended.Trim());
        AddParameter(command, originalPatientName.Trim());
        AddParameter(command, originalDate.Date);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(string patientName, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM [observations] WHERE [Patient_Name] = ? AND [Date] = ?";
        AddParameter(command, patientName.Trim());
        AddParameter(command, date.Date);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Observation MapObservation(System.Data.Common.DbDataReader reader)
    {
        return new Observation
        {
            Date = TryDate(reader["Date"]) ?? DateTime.Today,
            Time = reader["Time"]?.ToString() ?? string.Empty,
            PatientName = reader["Patient_Name"]?.ToString() ?? string.Empty,
            Age = TryInt(reader["Age"]),
            Sex = reader["Sex"]?.ToString() ?? string.Empty,
            Problem = reader["problem"]?.ToString() ?? string.Empty,
            ObservationText = reader["Observation"]?.ToString() ?? string.Empty,
            TestsRecommended = reader["testsrecom"]?.ToString() ?? string.Empty
        };
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static DateTime? TryDate(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        if (DateTime.TryParse(value.ToString(), out var parsed))
        {
            return parsed;
        }

        return null;
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