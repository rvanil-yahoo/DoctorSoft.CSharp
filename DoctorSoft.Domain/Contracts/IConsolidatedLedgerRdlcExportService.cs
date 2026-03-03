using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IConsolidatedLedgerRdlcExportService
{
    Task<byte[]> ExportPdfAsync(IReadOnlyList<ConsolidatedLedgerRecord> rows, string reportTitle, CancellationToken cancellationToken = default);
}
