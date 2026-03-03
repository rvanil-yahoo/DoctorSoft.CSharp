using DoctorSoft.Data.Security;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;
using DoctorSoft.Domain.Services;
using DoctorSoft.Reports.Services;

namespace DoctorSoft.Tests;

public class UnitTest1
{
    [Fact]
    public void LegacyPasswordDecoder_Decodes_Vb6EncodedCredentials()
    {
        var decoder = new LegacyPasswordDecoder();
        var encoded = EncodeLegacy("admin,pass;");

        var decoded = decoder.Decode(encoded);

        Assert.Equal("admin,pass;", decoded);
    }

    [Fact]
    public async Task AuthenticationService_ReturnsSuccess_ForMatchingPassword()
    {
        var decoder = new LegacyPasswordDecoder();
        var store = new InMemoryUserStore(new UserAccount
        {
            UserName = "admin",
            EncodedPassword = EncodeLegacy("admin,pass;")
        });
        var auth = new AuthenticationService(store, decoder);

        var result = await auth.SignInAsync("admin", "pass");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task AppointmentRdlcExportService_ExportsPdfBytes()
    {
        var exporter = new AppointmentRdlcExportService();
        var rows = new List<AppointmentReportRecord>
        {
            new()
            {
                StartDate = new DateTime(2026, 3, 2),
                AppTime = "10:30 AM",
                PatientName = "Test Patient",
                EventTitle = "Consultation",
                EventDetails = "Routine check",
                Status = true
            }
        };

        var pdfBytes = await exporter.ExportPdfAsync(rows, "Appointment Export Test");

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal('%', (char)pdfBytes[0]);
        Assert.Equal('P', (char)pdfBytes[1]);
        Assert.Equal('D', (char)pdfBytes[2]);
        Assert.Equal('F', (char)pdfBytes[3]);
    }

    [Fact]
    public async Task PaymentRdlcExportService_ExportsPdfBytes()
    {
        var exporter = new PaymentRdlcExportService();
        var rows = new List<PaymentReportRecord>
        {
            new()
            {
                VoucherNo = 101,
                VoucherDate = new DateTime(2026, 3, 2),
                PaidTowards = "Expenses",
                ReceiverName = "Vendor",
                AmountPaid = 250m,
                ExpenditureCause = "Supplies"
            }
        };

        var pdfBytes = await exporter.ExportPdfAsync(rows, "Payment Export Test");

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal('%', (char)pdfBytes[0]);
        Assert.Equal('P', (char)pdfBytes[1]);
        Assert.Equal('D', (char)pdfBytes[2]);
        Assert.Equal('F', (char)pdfBytes[3]);
    }

    [Fact]
    public async Task ReceiptRdlcExportService_ExportsPdfBytes()
    {
        var exporter = new ReceiptRdlcExportService();
        var rows = new List<ReceiptReportRecord>
        {
            new()
            {
                VoucherNo = 55,
                VoucherDate = new DateTime(2026, 3, 2),
                ReceiverName = "Patient A",
                LedgerName = "Consultation",
                AmountReceived = 500m,
                DoctorName = "Dr X"
            }
        };

        var pdfBytes = await exporter.ExportPdfAsync(rows, "Receipt Export Test");

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal('%', (char)pdfBytes[0]);
        Assert.Equal('P', (char)pdfBytes[1]);
        Assert.Equal('D', (char)pdfBytes[2]);
        Assert.Equal('F', (char)pdfBytes[3]);
    }

    [Fact]
    public async Task ConsolidatedLedgerRdlcExportService_ExportsPdfBytes()
    {
        var exporter = new ConsolidatedLedgerRdlcExportService();
        var rows = new List<ConsolidatedLedgerRecord>
        {
            new()
            {
                Date = new DateTime(2026, 3, 2),
                VoucherNo = 10,
                LedgerName = "Consultation",
                Debit = 0m,
                Credit = 700m,
                RunningBalance = 700m
            }
        };

        var pdfBytes = await exporter.ExportPdfAsync(rows, "Ledger Export Test");

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal('%', (char)pdfBytes[0]);
        Assert.Equal('P', (char)pdfBytes[1]);
        Assert.Equal('D', (char)pdfBytes[2]);
        Assert.Equal('F', (char)pdfBytes[3]);
    }

    private static string EncodeLegacy(string plainText)
    {
        var segments = plainText.Select(ch => $"${(int)ch / 7.0}$");
        return string.Concat(segments);
    }

    private sealed class InMemoryUserStore : IUserCredentialStore
    {
        private readonly UserAccount userAccount;

        public InMemoryUserStore(UserAccount userAccount)
        {
            this.userAccount = userAccount;
        }

        public Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                string.Equals(userAccount.UserName, userName, StringComparison.OrdinalIgnoreCase)
                    ? userAccount
                    : null);
        }
    }
}