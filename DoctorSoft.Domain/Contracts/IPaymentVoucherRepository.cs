using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPaymentVoucherRepository
{
    Task<int> GetNextVoucherNoAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PaymentVoucher voucher, CancellationToken cancellationToken = default);
}