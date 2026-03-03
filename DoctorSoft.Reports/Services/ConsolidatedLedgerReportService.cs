using DoctorSoft.Data.Access;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class ConsolidatedLedgerReportService : IConsolidatedLedgerReportService
{
    private readonly IOleDbConnectionFactory connectionFactory;

    public ConsolidatedLedgerReportService(IOleDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public Task<IReadOnlyList<ConsolidatedLedgerRecord>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "l.[ldate] BETWEEN ? AND ?",
            parameterBuilder: command =>
            {
                AddParameter(command, fromDate.Date);
                AddParameter(command, toDate.Date);
            },
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<ConsolidatedLedgerRecord>> GetByDateRangeAndLedgerAsync(DateTime fromDate, DateTime toDate, string ledgerName, CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            whereSql: "l.[ldate] BETWEEN ? AND ? AND l.[lname] = ?",
            parameterBuilder: command =>
            {
                AddParameter(command, fromDate.Date);
                AddParameter(command, toDate.Date);
                AddParameter(command, ledgerName.Trim());
            },
            cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<ConsolidatedLedgerRecord>> QueryAsync(
        string whereSql,
        Action<System.Data.Common.DbCommand> parameterBuilder,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT l.[autoid], l.[ldate], l.[lno], l.[lname], l.[debit], l.[credit], l.[coex], " +
            "d.[ClinicName], d.[DoctorName], d.[ClinicAddr], d.[ClinicPhone] " +
            "FROM [ledger] l, [DoctorDetails] d WHERE " + whereSql + " ORDER BY l.[ldate], l.[autoid]";

        parameterBuilder(command);

        var list = new List<ConsolidatedLedgerRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader is null)
        {
            return list;
        }

        decimal runningBalance = 0m;
        while (await reader.ReadAsync(cancellationToken))
        {
            var debit = TryDecimal(reader["debit"]);
            var credit = TryDecimal(reader["credit"]);
            var net = credit - debit;
            runningBalance += net;

            list.Add(new ConsolidatedLedgerRecord
            {
                AutoId = TryInt(reader["autoid"]),
                Date = TryDate(reader["ldate"]) ?? DateTime.MinValue,
                VoucherNo = TryInt(reader["lno"]),
                LedgerName = reader["lname"]?.ToString() ?? string.Empty,
                Debit = debit,
                Credit = credit,
                NetAmount = net,
                RunningBalance = runningBalance,
                Narration = reader["coex"]?.ToString() ?? string.Empty,
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