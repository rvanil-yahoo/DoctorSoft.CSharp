using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IAppointmentReportService
{
    Task<IReadOnlyList<AppointmentReportRecord>> GetAppointReportAsync(DateTime startDate, string? patientName = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentReportRecord>> GetAllAppByDateReportAsync(DateTime startDate, bool completedOnly, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentReportRecord>> GetAllAppReportAsync(CancellationToken cancellationToken = default);
}
