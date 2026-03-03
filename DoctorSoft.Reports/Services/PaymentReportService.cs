using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class PaymentReportService : IPaymentReportService
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public PaymentReportService(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public Task<IReadOnlyList<PaymentReportRecord>> GetByVoucherNoAsync(int voucherNo, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "p.[pno] = ?",
            parameterBuilder: command => AddParameter(command, voucherNo),
            orderBy: "p.[pdate], p.[pno]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<PaymentReportRecord>> GetByPaymentNameAsync(string paymentName, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "p.[pname] = ?",
            parameterBuilder: command => AddParameter(command, paymentName.Trim()),
            orderBy: "p.[pdate], p.[pno]",
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<PaymentReportRecord>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "p.[pdate] BETWEEN ? AND ?",
            parameterBuilder: command =>
            {
                AddParameter(command, fromDate.Date);
                AddParameter(command, toDate.Date);
            },
            orderBy: "p.[pdate], p.[pno]",
            cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<PaymentReportRecord>> QueryAsync(
        string whereSql,
        Action<System.Data.Common.DbCommand> parameterBuilder,
        string orderBy,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT p.[pno], p.[pdate], p.[pname], p.[prec], p.[pby], p.[amtpd], p.[coex], p.[pmon], " +
            "d.[ClinicName], d.[DoctorName], d.[ClinicAddr], d.[ClinicPhone] " +
            "FROM [payment] p, [DoctorDetails] d " +
            "WHERE " + whereSql + " ORDER BY " + orderBy;

        parameterBuilder(command);

        var list = new List<PaymentReportRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PaymentReportRecord
            {
                VoucherNo = TryInt(reader["pno"]) ?? 0,
                VoucherDate = TryDate(reader["pdate"]) ?? DateTime.MinValue,
                PaidTowards = reader["pname"]?.ToString() ?? string.Empty,
                ReceiverName = reader["prec"]?.ToString() ?? string.Empty,
                PaidBy = reader["pby"]?.ToString() ?? string.Empty,
                AmountPaid = TryDecimal(reader["amtpd"]),
                ExpenditureCause = reader["coex"]?.ToString() ?? string.Empty,
                MonthName = reader["pmon"]?.ToString() ?? string.Empty,
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