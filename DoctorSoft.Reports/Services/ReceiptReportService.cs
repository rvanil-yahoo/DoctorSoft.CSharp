using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class ReceiptReportService : IReceiptReportService
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public ReceiptReportService(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public Task<IReadOnlyList<ReceiptReportRecord>> GetByVoucherNoAsync(int voucherNo, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "r.[rno] = ?",
            parameterBuilder: command => AddParameter(command, voucherNo),
            orderBy: "r.[rdate], r.[rno]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<ReceiptReportRecord>> GetByLedgerNameAsync(string ledgerName, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "r.[lname] = ?",
            parameterBuilder: command => AddParameter(command, ledgerName.Trim()),
            orderBy: "r.[rdate], r.[rno]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<ReceiptReportRecord>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "r.[rdate] BETWEEN ? AND ?",
            parameterBuilder: command =>
            {
                AddParameter(command, fromDate.Date);
                AddParameter(command, toDate.Date);
            },
            orderBy: "r.[rdate], r.[rno]",
            cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<ReceiptReportRecord>> QueryAsync(
        string whereSql,
        Action<System.Data.Common.DbCommand> parameterBuilder,
        string orderBy,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT r.[rno], r.[rdate], r.[rname], r.[lname], r.[amtpd], d.[ClinicName], d.[DoctorName], d.[ClinicAddr], d.[ClinicPhone] " +
            "FROM [reciepts] r, [DoctorDetails] d WHERE " + whereSql + " ORDER BY " + orderBy;

        parameterBuilder(command);

        var list = new List<ReceiptReportRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ReceiptReportRecord
            {
                VoucherNo = TryInt(reader["rno"]) ?? 0,
                VoucherDate = TryDate(reader["rdate"]) ?? DateTime.MinValue,
                ReceiverName = reader["rname"]?.ToString() ?? string.Empty,
                LedgerName = reader["lname"]?.ToString() ?? string.Empty,
                AmountReceived = TryDecimal(reader["amtpd"]),
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