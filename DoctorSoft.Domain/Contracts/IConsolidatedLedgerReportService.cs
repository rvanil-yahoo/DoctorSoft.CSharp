using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IConsolidatedLedgerReportService
{
    Task<IReadOnlyList<ConsolidatedLedgerRecord>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsolidatedLedgerRecord>> GetByDateRangeAndLedgerAsync(DateTime fromDate, DateTime toDate, string ledgerName, CancellationToken cancellationToken = default);
}