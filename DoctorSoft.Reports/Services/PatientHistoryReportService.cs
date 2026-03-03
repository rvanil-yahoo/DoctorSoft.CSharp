using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class PatientHistoryReportService : IPatientHistoryReportService
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public PatientHistoryReportService(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PatientHistoryReportRecord>> GetByPatientAsync(string patientName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT h.[Patient_Name], h.[Test_Date], h.[Test_Name], h.[Test_Description], h.[Observations], " +
            "d.[ClinicName], d.[DoctorName], d.[ClinicAddr], d.[ClinicPhone] " +
            "FROM [history] h, [DoctorDetails] d " +
            "WHERE h.[Patient_Name] = ? " +
            "ORDER BY h.[Test_Date]";

        AddParameter(command, patientName.Trim());

        var list = new List<PatientHistoryReportRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PatientHistoryReportRecord
            {
                PatientName = reader["Patient_Name"]?.ToString() ?? string.Empty,
                TestDate = TryDate(reader["Test_Date"]) ?? DateTime.MinValue,
                TestName = reader["Test_Name"]?.ToString() ?? string.Empty,
                TestDescription = reader["Test_Description"]?.ToString() ?? string.Empty,
                Observations = reader["Observations"]?.ToString() ?? string.Empty,
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
}