using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class PrescriptionReportService : IPrescriptionReportService
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public PrescriptionReportService(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public Task<IReadOnlyList<PrescriptionReportRecord>> GetByPrescriptionIdAsync(int prescId, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "pm.[Presc_Id] = ?",
            parameterBuilder: command => AddParameter(command, prescId),
            orderBy: "pr.[Medicine]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<PrescriptionReportRecord>> GetByPatientAsync(string patientName, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "pm.[Patient_Name] = ?",
            parameterBuilder: command => AddParameter(command, patientName.Trim()),
            orderBy: "pm.[Date], pm.[Time], pr.[Medicine]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<PrescriptionReportRecord>> GetByDateAsync(DateTime date, string? patientName = null, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "pm.[Date] = ?" + (string.IsNullOrWhiteSpace(patientName) ? string.Empty : " AND pm.[Patient_Name] = ?"),
            parameterBuilder: command =>
            {
                AddParameter(command, date.Date);
                if (!string.IsNullOrWhiteSpace(patientName))
                {
                    AddParameter(command, patientName.Trim());
                }
            },
            orderBy: "pm.[Patient_Name], pr.[Medicine]",
            cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<PrescriptionReportRecord>> QueryAsync(
        string whereSql,
        Action<System.Data.Common.DbCommand> parameterBuilder,
        string orderBy,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT pm.[Presc_Id], pm.[Patient_Name], pm.[Patient_Address], pm.[Patient_Age], pm.[Date], pm.[Time], " +
            "pr.[Medicine], pr.[Type], pr.[Dosage], pr.[Quantity], d.[ClinicName], d.[DoctorName], d.[ClinicAddr], d.[ClinicPhone] " +
            "FROM [Presc_Main] pm, [Presc_Ref] pr, [DoctorDetails] d " +
            "WHERE pm.[Presc_Id] = pr.[Presc_Id] AND " + whereSql +
            " ORDER BY " + orderBy;

        parameterBuilder(command);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<PrescriptionReportRecord>();
        if (reader is null)
        {
            return rows;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PrescriptionReportRecord
            {
                PrescId = TryInt(reader["Presc_Id"]) ?? 0,
                PatientName = reader["Patient_Name"]?.ToString() ?? string.Empty,
                PatientAddress = reader["Patient_Address"]?.ToString() ?? string.Empty,
                PatientAge = TryInt(reader["Patient_Age"]),
                Date = TryDate(reader["Date"]) ?? DateTime.MinValue,
                Time = reader["Time"]?.ToString() ?? string.Empty,
                Medicine = reader["Medicine"]?.ToString() ?? string.Empty,
                Type = reader["Type"]?.ToString() ?? string.Empty,
                Dosage = reader["Dosage"]?.ToString() ?? string.Empty,
                Quantity = reader["Quantity"]?.ToString() ?? string.Empty,
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

    private static int? TryInt(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
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
