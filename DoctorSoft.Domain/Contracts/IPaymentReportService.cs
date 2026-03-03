using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPaymentReportService
{
    Task<IReadOnlyList<PaymentReportRecord>> GetByVoucherNoAsync(int voucherNo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentReportRecord>> GetByPaymentNameAsync(string paymentName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentReportRecord>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}