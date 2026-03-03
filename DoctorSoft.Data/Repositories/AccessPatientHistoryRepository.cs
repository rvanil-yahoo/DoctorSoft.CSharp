using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessPatientHistoryRepository : IPatientHistoryRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessPatientHistoryRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PatientHistoryEntry>> SearchAsync(string? patientNameFilter, DateTime? testDateFilter, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT [Patient_Name], [Test_Date], [Test_Name], [Test_Description], [Observations] FROM [history] WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(patientNameFilter))
        {
            command.CommandText += " AND [Patient_Name] LIKE ?";
            AddParameter(command, $"*{patientNameFilter.Trim()}*");
        }

        if (testDateFilter.HasValue)
        {
            command.CommandText += " AND [Test_Date] = ?";
            AddParameter(command, testDateFilter.Value.Date);
        }

        command.CommandText += " ORDER BY [Patient_Name], [Test_Date]";

        var list = new List<PatientHistoryEntry>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PatientHistoryEntry
            {
                PatientName = reader["Patient_Name"]?.ToString() ?? string.Empty,
                TestDate = TryDate(reader["Test_Date"]) ?? DateTime.Today,
                TestName = reader["Test_Name"]?.ToString() ?? string.Empty,
                TestDescription = reader["Test_Description"]?.ToString() ?? string.Empty,
                Observations = reader["Observations"]?.ToString() ?? string.Empty
            });
        }

        return list;
    }

    public async Task<bool> ExistsAsync(string patientName, DateTime testDate, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [history] WHERE [Patient_Name] = ? AND [Test_Date] = ?";
        AddParameter(command, patientName.Trim());
        AddParameter(command, testDate.Date);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value) > 0;
    }

    public async Task AddAsync(PatientHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO [history] ([Patient_Name], [Test_Date], [Test_Name], [Test_Description], [Observations]) VALUES (?, ?, ?, ?, ?)";
        AddParameter(command, entry.PatientName.Trim());
        AddParameter(command, entry.TestDate.Date);
        AddParameter(command, entry.TestName.Trim());
        AddParameter(command, entry.TestDescription.Trim());
        AddParameter(command, entry.Observations.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(PatientHistoryEntry entry, string originalPatientName, DateTime originalTestDate, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE [history] SET [Patient_Name] = ?, [Test_Date] = ?, [Test_Name] = ?, [Test_Description] = ?, [Observations] = ? " +
            "WHERE [Patient_Name] = ? AND [Test_Date] = ?";

        AddParameter(command, entry.PatientName.Trim());
        AddParameter(command, entry.TestDate.Date);
        AddParameter(command, entry.TestName.Trim());
        AddParameter(command, entry.TestDescription.Trim());
        AddParameter(command, entry.Observations.Trim());
        AddParameter(command, originalPatientName.Trim());
        AddParameter(command, originalTestDate.Date);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(string patientName, DateTime testDate, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM [history] WHERE [Patient_Name] = ? AND [Test_Date] = ?";
        AddParameter(command, patientName.Trim());
        AddParameter(command, testDate.Date);

        await command.ExecuteNonQueryAsync(cancellationToken);
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

        return DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }
}