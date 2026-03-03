namespace DoctorSoft.Domain.Models;

public sealed class PaymentVoucher
{
    public int VoucherNo { get; set; }
    public DateTime VoucherDate { get; set; }
    public string PaidTowards { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string PaidBy { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string ExpenditureCause { get; set; } = string.Empty;
}