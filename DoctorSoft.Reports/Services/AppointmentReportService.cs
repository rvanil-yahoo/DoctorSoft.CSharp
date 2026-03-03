using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class AppointmentReportService : IAppointmentReportService
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AppointmentReportService(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public Task<IReadOnlyList<AppointmentReportRecord>> GetAppointReportAsync(DateTime startDate, string? patientName = null, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "a.[Start_Date] = ?" + (string.IsNullOrWhiteSpace(patientName) ? string.Empty : " AND a.[Patient_Name] = ?"),
            parameterBuilder: command =>
            {
                AddParameter(command, startDate.Date);
                if (!string.IsNullOrWhiteSpace(patientName))
                {
                    AddParameter(command, patientName.Trim());
                }
            },
            orderBy: "a.[App_Time], a.[Patient_Name]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<AppointmentReportRecord>> GetAllAppByDateReportAsync(DateTime startDate, bool completedOnly, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "a.[Start_Date] = ?" + (completedOnly ? " AND a.[Status] = ?" : string.Empty),
            parameterBuilder: command =>
            {
                AddParameter(command, startDate.Date);
                if (completedOnly)
                {
                    AddParameter(command, true);
                }
            },
            orderBy: "a.[App_Time], a.[Patient_Name]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<AppointmentReportRecord>> GetAllAppReportAsync(CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: null,
            parameterBuilder: null,
            orderBy: "a.[Start_Date], a.[App_Time], a.[Patient_Name]",
            cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<AppointmentReportRecord>> QueryAsync(
        string? whereSql,
        Action<System.Data.Common.DbCommand>? parameterBuilder,
        string orderBy,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT a.[Start_Date], a.[App_Time], a.[Patient_Name], a.[Event_Title], a.[Event_Details], a.[Status], " +
            "d.[ClinicName], d.[DoctorName], d.[ClinicAddr], d.[ClinicPhone] " +
            "FROM [appointment] a, [DoctorDetails] d";

        if (!string.IsNullOrWhiteSpace(whereSql))
        {
            command.CommandText += " WHERE " + whereSql;
        }

        command.CommandText += " ORDER BY " + orderBy;

        parameterBuilder?.Invoke(command);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<AppointmentReportRecord>();
        if (reader is null)
        {
            return rows;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AppointmentReportRecord
            {
                StartDate = TryDate(reader["Start_Date"]) ?? DateTime.MinValue,
                AppTime = reader["App_Time"]?.ToString() ?? string.Empty,
                PatientName = reader["Patient_Name"]?.ToString() ?? string.Empty,
                EventTitle = reader["Event_Title"]?.ToString() ?? string.Empty,
                EventDetails = reader["Event_Details"]?.ToString() ?? string.Empty,
                Status = TryBool(reader["Status"]),
                ClinicName = reader["ClinicName"]?.ToString() ?? string.Empty,
                DoctorName = reader["DoctorName"]?.ToString() ?? string.Empty,
                ClinicAddr = reader["ClinicAddr"]?.ToString() ?? string.Empty,
                ClinicPhone = reader["ClinicPhone"]?.ToString() ?? string.Empty
            });
        }

        return rows;
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
