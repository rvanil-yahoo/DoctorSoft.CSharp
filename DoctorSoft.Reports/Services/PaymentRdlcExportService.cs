using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.Reports.Services;

public sealed class PaymentRdlcExportService : IPaymentRdlcExportService
{
    public Task<byte[]> ExportPdfAsync(IReadOnlyList<PaymentReportRecord> rows, string reportTitle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tableRows = rows.Select(row => new TabularSixColumnRdlcRenderer.TabularRow
        {
            Col1 = row.VoucherDate == DateTime.MinValue ? string.Empty : row.VoucherDate.ToString("yyyy-MM-dd"),
            Col2 = row.VoucherNo.ToString(),
            Col3 = row.PaidTowards,
            Col4 = row.ReceiverName,
            Col5 = row.AmountPaid.ToString("N2"),
            Col6 = row.ExpenditureCause
        }).ToList();

        var bytes = TabularSixColumnRdlcRenderer.RenderPdf(
            tableRows,
            string.IsNullOrWhiteSpace(reportTitle) ? "Payment Report" : reportTitle,
            "Date",
            "Voucher",
            "Paid Towards",
            "Receiver",
            "Amount",
            "Cause");

        return Task.FromResult(bytes);
    }
}
