using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Data.Repositories;

public sealed class AccessPaymentVoucherRepository : IPaymentVoucherRepository
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public AccessPaymentVoucherRepository(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<int> GetNextVoucherNoAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX([pno]) FROM [payment]";

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null || value == DBNull.Value)
        {
            return 1;
        }

        return Convert.ToInt32(value) + 1;
    }

    public async Task AddAsync(PaymentVoucher voucher, CancellationToken cancellationToken = default)
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
                    "INSERT INTO [payment] ([pdate], [pno], [pname], [pmon], [prec], [pby], [amtpd], [coex]) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                AddParameter(command, voucher.VoucherDate.Date);
                AddParameter(command, voucher.VoucherNo);
                AddParameter(command, voucher.PaidTowards.Trim());
                AddParameter(command, voucher.VoucherDate.ToString("MMMM"));
                AddParameter(command, voucher.ReceiverName.Trim());
                AddParameter(command, voucher.PaidBy.Trim());
                AddParameter(command, voucher.AmountPaid);
                AddParameter(command, voucher.ExpenditureCause.Trim());

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            using (var ledgerCommand = connection.CreateCommand())
            {
                ledgerCommand.Transaction = transaction;
                ledgerCommand.CommandText =
                    "INSERT INTO [ledger] ([lno], [lname], [ldate], [debit], [coex]) VALUES (?, ?, ?, ?, ?)";

                AddParameter(ledgerCommand, voucher.VoucherNo);
                AddParameter(ledgerCommand, voucher.PaidTowards.Trim());
                AddParameter(ledgerCommand, voucher.VoucherDate.Date);
                AddParameter(ledgerCommand, voucher.AmountPaid);
                AddParameter(ledgerCommand, voucher.ExpenditureCause.Trim());

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