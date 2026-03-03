using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class ConsolidatedLedgerRdlcExportService : IConsolidatedLedgerRdlcExportService
{
    public Task<byte[]> ExportPdfAsync(IReadOnlyList<ConsolidatedLedgerRecord> rows, string reportTitle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tableRows = rows.Select(row => new TabularSixColumnRdlcRenderer.TabularRow
        {
            Col1 = row.Date == DateTime.MinValue ? string.Empty : row.Date.ToString("yyyy-MM-dd"),
            Col2 = row.VoucherNo?.ToString() ?? string.Empty,
            Col3 = row.LedgerName,
            Col4 = row.Debit.ToString("N2"),
            Col5 = row.Credit.ToString("N2"),
            Col6 = row.RunningBalance.ToString("N2")
        }).ToList();

        var bytes = TabularSixColumnRdlcRenderer.RenderPdf(
            tableRows,
            string.IsNullOrWhiteSpace(reportTitle) ? "Consolidated Ledger" : reportTitle,
            "Date",
            "Voucher",
            "Ledger",
            "Debit",
            "Credit",
            "Balance");

        return Task.FromResult(bytes);
    }
}
