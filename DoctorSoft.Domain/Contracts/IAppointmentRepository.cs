using DoctorSoft.Domain.Models;

namespace DoctorSoft.Domain.Contracts;

public interface IAppointmentRepository
{
    Task<IReadOnlyList<Appointment>> SearchAsync(DateTime? dateFilter, bool pendingOnly, string? patientNameFilter, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(DateTime startDate, string patientName, CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(DateTime startDate, string patientName, string appTime, CancellationToken cancellationToken = default);
}
