using System.Reflection;
using Microsoft.Reporting.NETCore;

namespace DoctorSoft.Reports.Services;

internal static class TabularSixColumnRdlcRenderer
{
    private const string TemplateResourceName = "DoctorSoft.Reports.Templates.TabularSixColumn.rdlc";

    public static byte[] RenderPdf(
        IReadOnlyList<TabularRow> rows,
        string reportTitle,
        string header1,
        string header2,
        string header3,
        string header4,
        string header5,
        string header6)
    {
        using var reportDefinition = GetTemplateStream();
        var report = new LocalReport();
        report.LoadReportDefinition(reportDefinition);

        report.DataSources.Add(new ReportDataSource("GenericDataSet", rows));
        report.SetParameters(new[]
        {
            new ReportParameter("ReportTitle", reportTitle),
            new ReportParameter("GeneratedOn", "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            new ReportParameter("Header1", header1),
            new ReportParameter("Header2", header2),
            new ReportParameter("Header3", header3),
            new ReportParameter("Header4", header4),
            new ReportParameter("Header5", header5),
            new ReportParameter("Header6", header6)
        });

        return report.Render("PDF");
    }

    private static Stream GetTemplateStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(TemplateResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException("Tabular RDLC template not found.");
        }

        return stream;
    }

    internal sealed class TabularRow
    {
        public string Col1 { get; init; } = string.Empty;
        public string Col2 { get; init; } = string.Empty;
        public string Col3 { get; init; } = string.Empty;
        public string Col4 { get; init; } = string.Empty;
        public string Col5 { get; init; } = string.Empty;
        public string Col6 { get; init; } = string.Empty;
    }
}
