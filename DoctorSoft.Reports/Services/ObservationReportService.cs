using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class ObservationReportService : IObservationReportService
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public ObservationReportService(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public Task<IReadOnlyList<ObservationReportRecord>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "o.[Date] = ?",
            parameterBuilder: command => AddParameter(command, date.Date),
            orderBy: "o.[Patient_Name], o.[Time]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<ObservationReportRecord>> GetByDateAndPatientAsync(DateTime date, string patientName, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "o.[Date] = ? AND o.[Patient_Name] = ?",
            parameterBuilder: command =>
            {
                AddParameter(command, date.Date);
                AddParameter(command, patientName.Trim());
            },
            orderBy: "o.[Time]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<ObservationReportRecord>> GetByPatientAsync(string patientName, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "o.[Patient_Name] = ?",
            parameterBuilder: command => AddParameter(command, patientName.Trim()),
            orderBy: "o.[Date], o.[Time]",
            cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<ObservationReportRecord>> QueryAsync(
        string whereSql,
        Action<System.Data.Common.DbCommand> parameterBuilder,
        string orderBy,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT o.[Date], o.[Time], o.[Patient_Name], o.[Age], o.[Sex], o.[problem], o.[Observation], o.[testsrecom], " +
            "d.[ClinicName], d.[DoctorName], d.[ClinicAddr], d.[ClinicPhone] " +
            "FROM [observations] o, [DoctorDetails] d " +
            "WHERE " + whereSql +
            " ORDER BY " + orderBy;

        parameterBuilder(command);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<ObservationReportRecord>();
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ObservationReportRecord
            {
                Date = TryDate(reader["Date"]) ?? DateTime.MinValue,
                Time = reader["Time"]?.ToString() ?? string.Empty,
                PatientName = reader["Patient_Name"]?.ToString() ?? string.Empty,
                Age = TryInt(reader["Age"]),
                Sex = reader["Sex"]?.ToString() ?? string.Empty,
                Problem = reader["problem"]?.ToString() ?? string.Empty,
                ObservationText = reader["Observation"]?.ToString() ?? string.Empty,
                TestsRecommended = reader["testsrecom"]?.ToString() ?? string.Empty,
                ClinicName = reader["ClinicName"]?.ToString() ?? string.Empty,
                DoctorName = reader["DoctorName"]?.ToString() ?? string.Empty,
                ClinicAddr = reader["ClinicAddr"]?.ToString() ?? string.Empty,
                ClinicPhone = reader["ClinicPhone"]?.ToString() ?? string.Empty
            });
        }

        return list;
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@p" + command.Parameters.Count;
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

    private static int? TryInt(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }
}