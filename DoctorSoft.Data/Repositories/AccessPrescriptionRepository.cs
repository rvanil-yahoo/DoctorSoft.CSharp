using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessPrescriptionRepository : IPrescriptionRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessPrescriptionRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<int> GetNextPrescriptionIdAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX([Presc_Id]) FROM [Presc_Main]";

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null || value == DBNull.Value)
        {
            return 1;
        }

        return Convert.ToInt32(value) + 1;
    }

    public async Task<bool> ExistsForPatientAndDateAsync(string patientName, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [Presc_Main] WHERE [Patient_Name] = ? AND [Date] = ?";
        AddParameter(command, patientName.Trim());
        AddParameter(command, date.Date);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value) > 0;
    }

    public async Task SaveAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO [Presc_Main] ([Presc_Id], [Patient_Name], [Patient_Address], [Patient_Age], [Date], [Time]) VALUES (?, ?, ?, ?, ?, ?)";
                AddParameter(command, prescription.PrescId);
                AddParameter(command, prescription.PatientName.Trim());
                AddParameter(command, prescription.PatientAddress.Trim());
                AddParameter(command, prescription.PatientAge.HasValue ? prescription.PatientAge.Value : DBNull.Value);
                AddParameter(command, prescription.Date.Date);
                AddParameter(command, prescription.Time.Trim());

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var line in prescription.Lines)
            {
                using var lineCommand = connection.CreateCommand();
                lineCommand.Transaction = transaction;
                lineCommand.CommandText = "INSERT INTO [Presc_Ref] ([Presc_Id], [Medicine], [Type], [Dosage], [Quantity]) VALUES (?, ?, ?, ?, ?)";
                AddParameter(lineCommand, line.PrescId);
                AddParameter(lineCommand, line.Medicine.Trim());
                AddParameter(lineCommand, line.Type.Trim());
                AddParameter(lineCommand, line.Dosage.Trim());
                AddParameter(lineCommand, line.Quantity.Trim());

                await lineCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
