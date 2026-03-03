using System.Diagnostics;

namespace DoctorSoft.App.Utilities;

internal static class PdfPreviewHelper
{
    public static string SaveToTemporaryPdf(byte[] pdfBytes, string filePrefix)
    {
        var safePrefix = string.IsNullOrWhiteSpace(filePrefix) ? "report" : filePrefix.Trim();
        var fileName = $"{safePrefix}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.pdf";
        var fullPath = Path.Combine(Path.GetTempPath(), fileName);

        File.WriteAllBytes(fullPath, pdfBytes);
        return fullPath;
    }

    public static void OpenWithDefaultViewer(string filePath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }
}
