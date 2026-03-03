using DoctorSoft.Domain.Models;
using DoctorSoft.Reports.Services;

namespace DoctorSoft.Tests;

public class RdlcExportServicesTests
{
    [Fact]
    public async Task AppointmentRdlcExportService_UsesDefaultTitleAndProducesPdf()
    {
        var exporter = new AppointmentRdlcExportService();
        var rows = new List<AppointmentReportRecord>
        {
            new()
            {
                StartDate = DateTime.MinValue,
                AppTime = "10:00",
                PatientName = "Patient",
                EventTitle = "Review",
                EventDetails = "Details",
                Status = false
            }
        };

        var pdf = await exporter.ExportPdfAsync(rows, " ");

        AssertPdf(pdf);
    }

    [Fact]
    public async Task PaymentRdlcExportService_UsesDefaultTitleAndProducesPdf()
    {
        var exporter = new PaymentRdlcExportService();
        var rows = new List<PaymentReportRecord>
        {
            new()
            {
                VoucherNo = 5,
                VoucherDate = DateTime.MinValue,
                PaidTowards = "Utilities",
                ReceiverName = "Vendor",
                AmountPaid = 123.45m,
                ExpenditureCause = "Cause"
            }
        };

        var pdf = await exporter.ExportPdfAsync(rows, "\t");

        AssertPdf(pdf);
    }

    [Fact]
    public async Task ReceiptRdlcExportService_UsesDefaultTitleAndProducesPdf()
    {
        var exporter = new ReceiptRdlcExportService();
        var rows = new List<ReceiptReportRecord>
        {
            new()
            {
                VoucherNo = 7,
                VoucherDate = DateTime.MinValue,
                ReceiverName = "Patient",
                LedgerName = "Consult",
                AmountReceived = 321.10m,
                DoctorName = "Dr Test"
            }
        };

        var pdf = await exporter.ExportPdfAsync(rows, " ");

        AssertPdf(pdf);
    }

    [Fact]
    public async Task ConsolidatedLedgerRdlcExportService_UsesDefaultTitleAndProducesPdf()
    {
        var exporter = new ConsolidatedLedgerRdlcExportService();
        var rows = new List<ConsolidatedLedgerRecord>
        {
            new()
            {
                Date = DateTime.MinValue,
                VoucherNo = null,
                LedgerName = "Consult",
                Debit = 0,
                Credit = 100,
                RunningBalance = 100
            }
        };

        var pdf = await exporter.ExportPdfAsync(rows, string.Empty);

        AssertPdf(pdf);
    }

    [Fact]
    public async Task Exporters_ThrowWhenCancellationRequested()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => new AppointmentRdlcExportService().ExportPdfAsync(Array.Empty<AppointmentReportRecord>(), "x", cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => new PaymentRdlcExportService().ExportPdfAsync(Array.Empty<PaymentReportRecord>(), "x", cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => new ReceiptRdlcExportService().ExportPdfAsync(Array.Empty<ReceiptReportRecord>(), "x", cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => new ConsolidatedLedgerRdlcExportService().ExportPdfAsync(Array.Empty<ConsolidatedLedgerRecord>(), "x", cts.Token));
    }

    private static void AssertPdf(byte[] pdf)
    {
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 4);
        Assert.Equal('%', (char)pdf[0]);
        Assert.Equal('P', (char)pdf[1]);
        Assert.Equal('D', (char)pdf[2]);
        Assert.Equal('F', (char)pdf[3]);
    }
}
