using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessAppointmentRepository : IAppointmentRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessAppointmentRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Appointment>> SearchAsync(DateTime? dateFilter, bool pendingOnly, string? patientNameFilter, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Date_Added], [Start_Date], [Event_Title], [Event_Details], [App_Time], [Patient_Name], [Patient_Address], [Patient_Age], [Patient_Sex], [Status] FROM [appointment] WHERE 1=1";

        if (dateFilter.HasValue)
        {
            command.CommandText += " AND [Start_Date] = ?";
            AddParameter(command, dateFilter.Value.Date);
        }

        if (pendingOnly)
        {
            command.CommandText += " AND [Status] = ?";
            AddParameter(command, false);
        }

        if (!string.IsNullOrWhiteSpace(patientNameFilter))
        {
            command.CommandText += " AND [Patient_Name] LIKE ?";
            AddParameter(command, $"*{patientNameFilter.Trim()}*");
        }

        command.CommandText += " ORDER BY [Start_Date], [App_Time], [Patient_Name]";

        var list = new List<Appointment>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapAppointment(reader));
        }

        return list;
    }

    public async Task<bool> ExistsAsync(DateTime startDate, string patientName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [appointment] WHERE [Start_Date] = ? AND [Patient_Name] = ?";
        AddParameter(command, startDate.Date);
        AddParameter(command, patientName.Trim());

        var count = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(count) > 0;
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO [appointment] ([Date_Added], [Start_Date], [Event_Title], [Event_Details], [App_Time], [Patient_Name], [Patient_Address], [Patient_Age], [Patient_Sex], [Status]) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

        AddParameter(command, appointment.DateAdded ?? DateTime.Now);
        AddParameter(command, appointment.StartDate.Date);
        AddParameter(command, appointment.EventTitle.Trim());
        AddParameter(command, appointment.EventDetails.Trim());
        AddParameter(command, appointment.AppTime.Trim());
        AddParameter(command, appointment.PatientName.Trim());
        AddParameter(command, appointment.PatientAddress.Trim());
        AddParameter(command, appointment.PatientAge.HasValue ? appointment.PatientAge.Value : DBNull.Value);
        AddParameter(command, appointment.PatientSex.Trim());
        AddParameter(command, appointment.Status);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkCompletedAsync(DateTime startDate, string patientName, string appTime, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE [appointment] SET [Status] = ? WHERE [Start_Date] = ? AND [Patient_Name] = ? AND [App_Time] = ?";
        AddParameter(command, true);
        AddParameter(command, startDate.Date);
        AddParameter(command, patientName.Trim());
        AddParameter(command, appTime.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static Appointment MapAppointment(System.Data.Common.DbDataReader reader)
    {
        return new Appointment
        {
            DateAdded = TryDate(reader["Date_Added"]),
            StartDate = TryDate(reader["Start_Date"]) ?? DateTime.MinValue,
            EventTitle = reader["Event_Title"]?.ToString() ?? string.Empty,
            EventDetails = reader["Event_Details"]?.ToString() ?? string.Empty,
            AppTime = reader["App_Time"]?.ToString() ?? string.Empty,
            PatientName = reader["Patient_Name"]?.ToString() ?? string.Empty,
            PatientAddress = reader["Patient_Address"]?.ToString() ?? string.Empty,
            PatientAge = TryInt(reader["Patient_Age"]),
            PatientSex = reader["Patient_Sex"]?.ToString() ?? string.Empty,
            Status = TryBool(reader["Status"])
        };
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
