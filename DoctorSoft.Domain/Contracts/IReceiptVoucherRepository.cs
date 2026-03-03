using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IReceiptVoucherRepository
{
    Task<int> GetNextVoucherNoAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ReceiptVoucher voucher, CancellationToken cancellationToken = default);
}