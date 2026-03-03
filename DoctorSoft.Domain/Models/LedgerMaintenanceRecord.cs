namespace DoctorSoft.Domain.Models;

public sealed class LedgerMaintenanceRecord
{
    public int AutoId { get; set; }
    public int? VoucherNo { get; set; }
    public DateTime Date { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Narration { get; set; } = string.Empty;
}