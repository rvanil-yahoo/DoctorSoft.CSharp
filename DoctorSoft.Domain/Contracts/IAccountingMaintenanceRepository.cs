using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IAccountingMaintenanceRepository
{
    Task<IReadOnlyList<PaymentMaintenanceRecord>> GetPaymentsAsync(DateTime? fromDate, DateTime? toDate, string? ledgerNameFilter);
    Task<IReadOnlyList<ReceiptMaintenanceRecord>> GetReceiptsAsync(DateTime? fromDate, DateTime? toDate, string? ledgerNameFilter);
    Task<IReadOnlyList<LedgerMaintenanceRecord>> GetLedgerEntriesAsync(DateTime? fromDate, DateTime? toDate, string? ledgerNameFilter);
    Task UpdatePaymentVoucherAsync(PaymentMaintenanceRecord record);
    Task UpdateReceiptVoucherAsync(ReceiptMaintenanceRecord record);
    Task UpdateLedgerEntryAsync(LedgerMaintenanceRecord record);
    Task DeletePaymentVoucherAsync(int voucherNo);
    Task DeleteReceiptVoucherAsync(int voucherNo);
    Task DeleteLedgerEntryAsync(int autoId);
}