using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IPaymentRdlcExportService
{
    Task<byte[]> ExportPdfAsync(IReadOnlyList<PaymentReportRecord> rows, string reportTitle, CancellationToken cancellationToken = default);
}
