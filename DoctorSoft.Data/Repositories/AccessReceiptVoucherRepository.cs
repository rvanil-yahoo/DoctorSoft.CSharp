using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessReceiptVoucherRepository : IReceiptVoucherRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessReceiptVoucherRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<int> GetNextVoucherNoAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX([rno]) FROM [reciepts]";

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null || value == DBNull.Value)
        {
            return 1;
        }

        return Convert.ToInt32(value) + 1;
    }

    public async Task AddAsync(ReceiptVoucher voucher, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "INSERT INTO [reciepts] ([rdate], [rno], [rname], [lname], [amtpd]) VALUES (?, ?, ?, ?, ?)";

                AddParameter(command, voucher.VoucherDate.Date);
                AddParameter(command, voucher.VoucherNo);
                AddParameter(command, voucher.ReceiverName.Trim());
                AddParameter(command, voucher.LedgerName.Trim());
                AddParameter(command, voucher.AmountReceived);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            using (var ledgerCommand = connection.CreateCommand())
            {
                ledgerCommand.Transaction = transaction;
                ledgerCommand.CommandText =
                    "INSERT INTO [ledger] ([lno], [lname], [ldate], [credit], [coex]) VALUES (?, ?, ?, ?, ?)";

                AddParameter(ledgerCommand, voucher.VoucherNo);
                AddParameter(ledgerCommand, voucher.LedgerName.Trim());
                AddParameter(ledgerCommand, voucher.VoucherDate.Date);
                AddParameter(ledgerCommand, voucher.AmountReceived);
                AddParameter(ledgerCommand, "Receipt Added from General Receipts for " + voucher.ReceiverName.Trim());

                await ledgerCommand.ExecuteNonQueryAsync(cancellationToken);
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