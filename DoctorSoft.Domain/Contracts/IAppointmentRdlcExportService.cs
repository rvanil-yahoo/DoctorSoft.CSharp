using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IAppointmentRdlcExportService
{
    Task<byte[]> ExportPdfAsync(IReadOnlyList<AppointmentReportRecord> rows, string reportTitle, CancellationToken cancellationToken = default);
}
