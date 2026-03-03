using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessAccountingMaintenanceRepository : IAccountingMaintenanceRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessAccountingMaintenanceRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PaymentMaintenanceRecord>> GetPaymentsAsync(DateTime? fromDate, DateTime? toDate, string? ledgerNameFilter)
    {
        var query = "SELECT [pno], [pdate], [pname], [prec], [pby], [amtpd], [coex] FROM [payment] WHERE 1=1";
        var parameterValues = new List<object>();

        if (fromDate.HasValue)
        {
            query += " AND [pdate] >= ?";
            parameterValues.Add(fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query += " AND [pdate] <= ?";
            parameterValues.Add(toDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(ledgerNameFilter))
        {
            query += " AND [pname] LIKE ?";
            parameterValues.Add($"%{ledgerNameFilter.Trim()}%");
        }

        query += " ORDER BY [pdate] DESC, [pno] DESC";

        using var connection = connectionFactory.Create();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = query;
        foreach (var value in parameterValues)
        {
            AddParameter(command, value);
        }

        var list = new List<PaymentMaintenanceRecord>();
        using var reader = await command.ExecuteReaderAsync();
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync())
        {
            list.Add(new PaymentMaintenanceRecord
            {
                VoucherNo = TryInt(reader["pno"]) ?? 0,
                VoucherDate = TryDate(reader["pdate"]) ?? DateTime.MinValue,
                PaidTowards = reader["pname"]?.ToString() ?? string.Empty,
                ReceiverName = reader["prec"]?.ToString() ?? string.Empty,
                PaidBy = reader["pby"]?.ToString() ?? string.Empty,
                AmountPaid = TryDecimal(reader["amtpd"]),
                ExpenditureCause = reader["coex"]?.ToString() ?? string.Empty
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<ReceiptMaintenanceRecord>> GetReceiptsAsync(DateTime? fromDate, DateTime? toDate, string? ledgerNameFilter)
    {
        var query = "SELECT [rno], [rdate], [rname], [lname], [amtpd] FROM [reciepts] WHERE 1=1";
        var parameterValues = new List<object>();

        if (fromDate.HasValue)
        {
            query += " AND [rdate] >= ?";
            parameterValues.Add(fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query += " AND [rdate] <= ?";
            parameterValues.Add(toDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(ledgerNameFilter))
        {
            query += " AND [lname] LIKE ?";
            parameterValues.Add($"%{ledgerNameFilter.Trim()}%");
        }

        query += " ORDER BY [rdate] DESC, [rno] DESC";

        using var connection = connectionFactory.Create();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = query;
        foreach (var value in parameterValues)
        {
            AddParameter(command, value);
        }

        var list = new List<ReceiptMaintenanceRecord>();
        using var reader = await command.ExecuteReaderAsync();
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync())
        {
            list.Add(new ReceiptMaintenanceRecord
            {
                VoucherNo = TryInt(reader["rno"]) ?? 0,
                VoucherDate = TryDate(reader["rdate"]) ?? DateTime.MinValue,
                ReceiverName = reader["rname"]?.ToString() ?? string.Empty,
                LedgerName = reader["lname"]?.ToString() ?? string.Empty,
                AmountReceived = TryDecimal(reader["amtpd"])
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<LedgerMaintenanceRecord>> GetLedgerEntriesAsync(DateTime? fromDate, DateTime? toDate, string? ledgerNameFilter)
    {
        var query = "SELECT [autoid], [lno], [ldate], [lname], [debit], [credit], [coex] FROM [ledger] WHERE 1=1";
        var parameterValues = new List<object>();

        if (fromDate.HasValue)
        {
            query += " AND [ldate] >= ?";
            parameterValues.Add(fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query += " AND [ldate] <= ?";
            parameterValues.Add(toDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(ledgerNameFilter))
        {
            query += " AND [lname] LIKE ?";
            parameterValues.Add($"%{ledgerNameFilter.Trim()}%");
        }

        query += " ORDER BY [ldate] DESC, [autoid] DESC";

        using var connection = connectionFactory.Create();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = query;
        foreach (var value in parameterValues)
        {
            AddParameter(command, value);
        }

        var list = new List<LedgerMaintenanceRecord>();
        using var reader = await command.ExecuteReaderAsync();
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync())
        {
            list.Add(new LedgerMaintenanceRecord
            {
                AutoId = TryInt(reader["autoid"]) ?? 0,
                VoucherNo = TryInt(reader["lno"]),
                Date = TryDate(reader["ldate"]) ?? DateTime.MinValue,
                LedgerName = reader["lname"]?.ToString() ?? string.Empty,
                Debit = TryDecimal(reader["debit"]),
                Credit = TryDecimal(reader["credit"]),
                Narration = reader["coex"]?.ToString() ?? string.Empty
            });
        }

        return list;
    }

    public async Task UpdatePaymentVoucherAsync(PaymentMaintenanceRecord record)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "UPDATE [payment] SET [pdate] = ?, [pname] = ?, [prec] = ?, [pby] = ?, [amtpd] = ?, [coex] = ? WHERE [pno] = ?";

                AddParameter(command, record.VoucherDate.Date);
                AddParameter(command, record.PaidTowards.Trim());
                AddParameter(command, record.ReceiverName.Trim());
                AddParameter(command, record.PaidBy.Trim());
                AddParameter(command, record.AmountPaid);
                AddParameter(command, record.ExpenditureCause.Trim());
                AddParameter(command, record.VoucherNo);

                await command.ExecuteNonQueryAsync();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "UPDATE [ledger] SET [ldate] = ?, [lname] = ?, [debit] = ?, [coex] = ? WHERE [lno] = ? AND Nz([debit], 0) <> 0";

                AddParameter(command, record.VoucherDate.Date);
                AddParameter(command, record.PaidTowards.Trim());
                AddParameter(command, record.AmountPaid);
                AddParameter(command, record.ExpenditureCause.Trim());
                AddParameter(command, record.VoucherNo);

                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateReceiptVoucherAsync(ReceiptMaintenanceRecord record)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "UPDATE [reciepts] SET [rdate] = ?, [rname] = ?, [lname] = ?, [amtpd] = ? WHERE [rno] = ?";

                AddParameter(command, record.VoucherDate.Date);
                AddParameter(command, record.ReceiverName.Trim());
                AddParameter(command, record.LedgerName.Trim());
                AddParameter(command, record.AmountReceived);
                AddParameter(command, record.VoucherNo);

                await command.ExecuteNonQueryAsync();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "UPDATE [ledger] SET [ldate] = ?, [lname] = ?, [credit] = ?, [coex] = ? WHERE [lno] = ? AND Nz([credit], 0) <> 0";

                AddParameter(command, record.VoucherDate.Date);
                AddParameter(command, record.LedgerName.Trim());
                AddParameter(command, record.AmountReceived);
                AddParameter(command, "Receipt Added from General Receipts for " + record.ReceiverName.Trim());
                AddParameter(command, record.VoucherNo);

                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateLedgerEntryAsync(LedgerMaintenanceRecord record)
    {
        var debit = record.Debit;
        var credit = record.Credit;

        if ((debit > 0m && credit > 0m) || (debit <= 0m && credit <= 0m))
        {
            throw new InvalidOperationException("Ledger row must have either debit or credit amount.");
        }

        if (string.IsNullOrWhiteSpace(record.LedgerName))
        {
            throw new InvalidOperationException("Ledger name is required.");
        }

        using var connection = connectionFactory.Create();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE [ledger] SET [ldate] = ?, [lname] = ?, [debit] = ?, [credit] = ?, [coex] = ? WHERE [autoid] = ?";

        AddParameter(command, record.Date.Date);
        AddParameter(command, record.LedgerName.Trim());
        AddParameter(command, debit > 0m ? debit : 0m);
        AddParameter(command, credit > 0m ? credit : 0m);
        AddParameter(command, record.Narration.Trim());
        AddParameter(command, record.AutoId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeletePaymentVoucherAsync(int voucherNo)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM [payment] WHERE [pno] = ?";
                AddParameter(command, voucherNo);
                await command.ExecuteNonQueryAsync();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM [ledger] WHERE [lno] = ? AND Nz([debit], 0) <> 0";
                AddParameter(command, voucherNo);
                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteReceiptVoucherAsync(int voucherNo)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM [reciepts] WHERE [rno] = ?";
                AddParameter(command, voucherNo);
                await command.ExecuteNonQueryAsync();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM [ledger] WHERE [lno] = ? AND Nz([credit], 0) <> 0";
                AddParameter(command, voucherNo);
                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteLedgerEntryAsync(int autoId)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM [ledger] WHERE [autoid] = ?";
        AddParameter(command, autoId);
        await command.ExecuteNonQueryAsync();
    }

    private static void AddParameter(System.Data.Common.DbCommand command, object value)
    {
        var parameter = command.CreateParameter();
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

    private static decimal TryDecimal(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return 0m;
        }

        return decimal.TryParse(value.ToString(), out var parsed) ? parsed : 0m;
    }
}