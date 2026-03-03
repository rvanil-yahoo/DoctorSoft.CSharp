using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IReceiptRdlcExportService
{
    Task<byte[]> ExportPdfAsync(IReadOnlyList<ReceiptReportRecord> rows, string reportTitle, CancellationToken cancellationToken = default);
}
