namespace DoctorSoft.Domain.Models;

public sealed class ConsolidatedLedgerRecord
{
    public int? AutoId { get; set; }
    public DateTime Date { get; set; }
    public int? VoucherNo { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal NetAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string Narration { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicAddr { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
}