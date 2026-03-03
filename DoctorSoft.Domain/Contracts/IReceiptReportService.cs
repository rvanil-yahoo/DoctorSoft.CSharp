using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IReceiptReportService
{
    Task<IReadOnlyList<ReceiptReportRecord>> GetByVoucherNoAsync(int voucherNo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceiptReportRecord>> GetByLedgerNameAsync(string ledgerName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceiptReportRecord>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}