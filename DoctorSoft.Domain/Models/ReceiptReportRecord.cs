namespace DoctorSoft.Domain.Models;

public sealed class ReceiptReportRecord
{
    public int VoucherNo { get; set; }
    public DateTime VoucherDate { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string LedgerName { get; set; } = string.Empty;
    public decimal AmountReceived { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicAddr { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
}