using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessReferralRepository : IReferralRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessReferralRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Referral>> SearchAsync(string? patientNameFilter, string? toDoctorFilter, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT [refdate], [pname], [paddr], [page], [psex], [fdoc], [fclin], [fclinaddr], [todoc], [toclin], [toaddr], [message] " +
            "FROM [refferral] WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(patientNameFilter))
        {
            command.CommandText += " AND [pname] LIKE ?";
            AddParameter(command, $"*{patientNameFilter.Trim()}*");
        }

        if (!string.IsNullOrWhiteSpace(toDoctorFilter))
        {
            command.CommandText += " AND [todoc] LIKE ?";
            AddParameter(command, $"*{toDoctorFilter.Trim()}*");
        }

        command.CommandText += " ORDER BY [refdate] DESC, [pname]";

        var list = new List<Referral>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapReferral(reader));
        }

        return list;
    }

    public async Task<Referral?> BuildDraftForPatientAsync(string patientName, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        string patientAddress = string.Empty;
        int? patientAge = null;
        string patientSex = string.Empty;

        using (var patientCommand = connection.CreateCommand())
        {
            patientCommand.CommandText =
                "SELECT [Patient_address], [Patient_age], [Patient_sex] FROM [patient] WHERE [Patient_Name] = ?";
            AddParameter(patientCommand, patientName.Trim());

            using var patientReader = await patientCommand.ExecuteReaderAsync(cancellationToken);
            if (patientReader is null || !await patientReader.ReadAsync(cancellationToken))
            {
                return null;
            }

            patientAddress = patientReader["Patient_address"]?.ToString() ?? string.Empty;
            patientAge = TryInt(patientReader["Patient_age"]);
            patientSex = patientReader["Patient_sex"]?.ToString() ?? string.Empty;
        }

        string fromDoctor = string.Empty;
        string fromClinic = string.Empty;
        string fromClinicAddress = string.Empty;

        using (var doctorCommand = connection.CreateCommand())
        {
            doctorCommand.CommandText =
                "SELECT TOP 1 [doctorname], [clinicname], [clinicaddr] FROM [doctordetails]";

            using var doctorReader = await doctorCommand.ExecuteReaderAsync(cancellationToken);
            if (doctorReader is not null && await doctorReader.ReadAsync(cancellationToken))
            {
                fromDoctor = doctorReader["doctorname"]?.ToString() ?? string.Empty;
                fromClinic = doctorReader["clinicname"]?.ToString() ?? string.Empty;
                fromClinicAddress = doctorReader["clinicaddr"]?.ToString() ?? string.Empty;
            }
        }

        return new Referral
        {
            RefDate = DateTime.Today,
            PatientName = patientName.Trim(),
            PatientAddress = patientAddress,
            PatientAge = patientAge,
            PatientSex = patientSex,
            FromDoctor = fromDoctor,
            FromClinic = fromClinic,
            FromClinicAddress = fromClinicAddress
        };
    }

    public async Task<bool> ExistsAsync(string patientName, DateTime refDate, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [refferral] WHERE [pname] = ? AND [refdate] = ?";
        AddParameter(command, patientName.Trim());
        AddParameter(command, refDate.Date);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value) > 0;
    }

    public async Task AddAsync(Referral referral, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO [refferral] ([pname], [paddr], [page], [psex], [fdoc], [fclin], [refdate], [fclinaddr], [todoc], [toclin], [toaddr], [message]) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

        AddParameter(command, referral.PatientName.Trim());
        AddParameter(command, referral.PatientAddress.Trim());
        AddParameter(command, referral.PatientAge.HasValue ? referral.PatientAge.Value : DBNull.Value);
        AddParameter(command, referral.PatientSex.Trim());
        AddParameter(command, referral.FromDoctor.Trim());
        AddParameter(command, referral.FromClinic.Trim());
        AddParameter(command, referral.RefDate.Date);
        AddParameter(command, referral.FromClinicAddress.Trim());
        AddParameter(command, referral.ToDoctor.Trim());
        AddParameter(command, referral.ToClinic.Trim());
        AddParameter(command, referral.ToAddress.Trim());
        AddParameter(command, referral.Message.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Referral referral, string originalPatientName, DateTime originalRefDate, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE [refferral] SET [pname] = ?, [paddr] = ?, [page] = ?, [psex] = ?, [fdoc] = ?, [fclin] = ?, [refdate] = ?, [fclinaddr] = ?, [todoc] = ?, [toclin] = ?, [toaddr] = ?, [message] = ? " +
            "WHERE [pname] = ? AND [refdate] = ?";

        AddParameter(command, referral.PatientName.Trim());
        AddParameter(command, referral.PatientAddress.Trim());
        AddParameter(command, referral.PatientAge.HasValue ? referral.PatientAge.Value : DBNull.Value);
        AddParameter(command, referral.PatientSex.Trim());
        AddParameter(command, referral.FromDoctor.Trim());
        AddParameter(command, referral.FromClinic.Trim());
        AddParameter(command, referral.RefDate.Date);
        AddParameter(command, referral.FromClinicAddress.Trim());
        AddParameter(command, referral.ToDoctor.Trim());
        AddParameter(command, referral.ToClinic.Trim());
        AddParameter(command, referral.ToAddress.Trim());
        AddParameter(command, referral.Message.Trim());
        AddParameter(command, originalPatientName.Trim());
        AddParameter(command, originalRefDate.Date);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(string patientName, DateTime refDate, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM [refferral] WHERE [pname] = ? AND [refdate] = ?";
        AddParameter(command, patientName.Trim());
        AddParameter(command, refDate.Date);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Referral MapReferral(System.Data.Common.DbDataReader reader)
    {
        return new Referral
        {
            RefDate = TryDate(reader["refdate"]) ?? DateTime.Today,
            PatientName = reader["pname"]?.ToString() ?? string.Empty,
            PatientAddress = reader["paddr"]?.ToString() ?? string.Empty,
            PatientAge = TryInt(reader["page"]),
            PatientSex = reader["psex"]?.ToString() ?? string.Empty,
            FromDoctor = reader["fdoc"]?.ToString() ?? string.Empty,
            FromClinic = reader["fclin"]?.ToString() ?? string.Empty,
            FromClinicAddress = reader["fclinaddr"]?.ToString() ?? string.Empty,
            ToDoctor = reader["todoc"]?.ToString() ?? string.Empty,
            ToClinic = reader["toclin"]?.ToString() ?? string.Empty,
            ToAddress = reader["toaddr"]?.ToString() ?? string.Empty,
            Message = reader["message"]?.ToString() ?? string.Empty
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