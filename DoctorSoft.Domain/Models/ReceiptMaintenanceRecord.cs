namespace DoctorSoft.Domain.Models;

public sealed class ReceiptMaintenanceRecord
{
    public int VoucherNo { get; set; }
    public DateTime VoucherDate { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string LedgerName { get; set; } = string.Empty;
    public decimal AmountReceived { get; set; }
}