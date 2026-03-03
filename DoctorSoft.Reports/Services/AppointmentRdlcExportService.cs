using System.Reflection;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;
using Microsoft.Reporting.NETCore;

namespace DoctorSoft.Reports.Services;

public sealed class AppointmentRdlcExportService : IAppointmentRdlcExportService
{
    private const string TemplateResourceName = "DoctorSoft.Reports.Templates.AppointmentSummary.rdlc";

    public Task<byte[]> ExportPdfAsync(IReadOnlyList<AppointmentReportRecord> rows, string reportTitle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var reportDefinition = GetTemplateStream();
        var report = new LocalReport();
        report.LoadReportDefinition(reportDefinition);

        var data = rows.Select(row => new AppointmentSummaryRow
        {
            StartDateText = row.StartDate == DateTime.MinValue ? string.Empty : row.StartDate.ToString("yyyy-MM-dd"),
            AppTime = row.AppTime,
            PatientName = row.PatientName,
            EventTitle = row.EventTitle,
            EventDetails = row.EventDetails,
            StatusText = row.Status ? "Completed" : "Pending"
        }).ToList();

        report.DataSources.Add(new ReportDataSource("AppointmentDataSet", data));
        report.SetParameters(new[]
        {
            new ReportParameter("ReportTitle", string.IsNullOrWhiteSpace(reportTitle) ? "Appointment Report" : reportTitle),
            new ReportParameter("GeneratedOn", "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
        });

        var bytes = report.Render("PDF");
        return Task.FromResult(bytes);
    }

    private static Stream GetTemplateStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(TemplateResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException("Appointment RDLC template not found.");
        }

        return stream;
    }

    private sealed class AppointmentSummaryRow
    {
        public string StartDateText { get; init; } = string.Empty;
        public string AppTime { get; init; } = string.Empty;
        public string PatientName { get; init; } = string.Empty;
        public string EventTitle { get; init; } = string.Empty;
        public string EventDetails { get; init; } = string.Empty;
        public string StatusText { get; init; } = string.Empty;
    }
}
