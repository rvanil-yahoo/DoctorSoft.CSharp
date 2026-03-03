using DoctorSoft.Domain.Contracts;
using DoctorSoft.App.Utilities;

namespace DoctorSoft.App.Forms;

public sealed class AppointmentReportsForm : Form
{
    private readonly IAppointmentReportService appointmentReportService;
    private readonly IAppointmentRdlcExportService appointmentRdlcExportService;
    private readonly ComboBox modeComboBox;
    private readonly DateTimePicker datePicker;
    private readonly TextBox patientNameTextBox;
    private readonly Button loadButton;
    private readonly Button previewButton;
    private readonly Button exportPdfButton;
    private readonly DataGridView reportGrid;
    private IReadOnlyList<Domain.Models.AppointmentReportRecord> currentRows = Array.Empty<Domain.Models.AppointmentReportRecord>();

    public AppointmentReportsForm(IAppointmentReportService appointmentReportService, IAppointmentRdlcExportService appointmentRdlcExportService)
    {
        this.appointmentReportService = appointmentReportService;
        this.appointmentRdlcExportService = appointmentRdlcExportService;

        Text = "Appointment Reports";
        Width = 1200;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 72 };
        modeComboBox = new ComboBox
        {
            Left = 20,
            Top = 24,
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        modeComboBox.Items.AddRange(new object[]
        {
            "Appoint (Date + optional patient)",
            "AllAppDt (Date, completed only)",
            "AllApp (All appointments)"
        });
        modeComboBox.SelectedIndex = 0;

        datePicker = new DateTimePicker
        {
            Left = 295,
            Top = 24,
            Width = 160,
            Format = DateTimePickerFormat.Short
        };

        patientNameTextBox = new TextBox
        {
            Left = 470,
            Top = 24,
            Width = 220,
            PlaceholderText = "Patient name (for Appoint)"
        };

        loadButton = new Button
        {
            Left = 705,
            Top = 22,
            Width = 120,
            Height = 30,
            Text = "Load"
        };
        loadButton.Click += async (_, _) => await LoadReportAsync();

        exportPdfButton = new Button
        {
            Left = 835,
            Top = 22,
            Width = 120,
            Height = 30,
            Text = "Export PDF"
        };
        exportPdfButton.Click += async (_, _) => await ExportPdfAsync();

        previewButton = new Button
        {
            Left = 965,
            Top = 22,
            Width = 120,
            Height = 30,
            Text = "Preview/Print"
        };
        previewButton.Click += async (_, _) => await PreviewPdfAsync();

        filterPanel.Controls.Add(modeComboBox);
        filterPanel.Controls.Add(datePicker);
        filterPanel.Controls.Add(patientNameTextBox);
        filterPanel.Controls.Add(loadButton);
        filterPanel.Controls.Add(exportPdfButton);
        filterPanel.Controls.Add(previewButton);

        reportGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        Controls.Add(reportGrid);
        Controls.Add(filterPanel);

        Shown += async (_, _) => await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        loadButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var mode = modeComboBox.SelectedIndex;
            IReadOnlyList<Domain.Models.AppointmentReportRecord> rows = mode switch
            {
                0 => await appointmentReportService.GetAppointReportAsync(datePicker.Value.Date, patientNameTextBox.Text),
                1 => await appointmentReportService.GetAllAppByDateReportAsync(datePicker.Value.Date, completedOnly: true),
                _ => await appointmentReportService.GetAllAppReportAsync()
            };

            currentRows = rows;
            reportGrid.DataSource = currentRows.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to load report data: {exception.Message}", "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private async Task PreviewPdfAsync()
    {
        if (currentRows.Count == 0)
        {
            MessageBox.Show("Load report data before previewing.", "Preview/Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        loadButton.Enabled = false;
        exportPdfButton.Enabled = false;
        previewButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var title = modeComboBox.SelectedIndex switch
            {
                0 => "Appoint Report",
                1 => "AllAppDt Report",
                _ => "AllApp Report"
            };

            var pdfBytes = await appointmentRdlcExportService.ExportPdfAsync(currentRows, title);
            var tempPath = PdfPreviewHelper.SaveToTemporaryPdf(pdfBytes, "appointment-report-preview");
            PdfPreviewHelper.OpenWithDefaultViewer(tempPath);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to preview report: {exception.Message}", "Preview/Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            exportPdfButton.Enabled = true;
            previewButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private async Task ExportPdfAsync()
    {
        if (currentRows.Count == 0)
        {
            MessageBox.Show("Load report data before exporting.", "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            FileName = $"appointment-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf",
            Title = "Export Appointment Report"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        loadButton.Enabled = false;
        exportPdfButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var title = modeComboBox.SelectedIndex switch
            {
                0 => "Appoint Report",
                1 => "AllAppDt Report",
                _ => "AllApp Report"
            };

            var pdfBytes = await appointmentRdlcExportService.ExportPdfAsync(currentRows, title);
            await File.WriteAllBytesAsync(dialog.FileName, pdfBytes);

            MessageBox.Show("PDF exported successfully.", "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to export PDF: {exception.Message}", "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            exportPdfButton.Enabled = true;
            UseWaitCursor = false;
        }
    }
}
