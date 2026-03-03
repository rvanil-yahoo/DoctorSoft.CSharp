using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class ReceiptRdlcExportService : IReceiptRdlcExportService
{
    public Task<byte[]> ExportPdfAsync(IReadOnlyList<ReceiptReportRecord> rows, string reportTitle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tableRows = rows.Select(row => new TabularSixColumnRdlcRenderer.TabularRow
        {
            Col1 = row.VoucherDate == DateTime.MinValue ? string.Empty : row.VoucherDate.ToString("yyyy-MM-dd"),
            Col2 = row.VoucherNo.ToString(),
            Col3 = row.ReceiverName,
            Col4 = row.LedgerName,
            Col5 = row.AmountReceived.ToString("N2"),
            Col6 = row.DoctorName
        }).ToList();

        var bytes = TabularSixColumnRdlcRenderer.RenderPdf(
            tableRows,
            string.IsNullOrWhiteSpace(reportTitle) ? "Receipt Report" : reportTitle,
            "Date",
            "Voucher",
            "Receiver",
            "Ledger",
            "Amount",
            "Doctor");

        return Task.FromResult(bytes);
    }
}
