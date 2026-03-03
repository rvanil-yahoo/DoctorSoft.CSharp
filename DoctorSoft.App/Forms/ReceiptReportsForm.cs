using DoctorSoft.Domain.Contracts;
using DoctorSoft.App.Utilities;

namespace DoctorSoft.App.Forms;

public sealed class ReceiptReportsForm : Form
{
    private readonly IReceiptReportService receiptReportService;
    private readonly IReceiptRdlcExportService receiptRdlcExportService;
    private readonly ComboBox modeComboBox;
    private readonly TextBox voucherNoTextBox;
    private readonly TextBox ledgerNameTextBox;
    private readonly DateTimePicker fromDatePicker;
    private readonly DateTimePicker toDatePicker;
    private readonly Button loadButton;
    private readonly Button previewButton;
    private readonly Button exportPdfButton;
    private readonly DataGridView reportGrid;
    private IReadOnlyList<Domain.Models.ReceiptReportRecord> currentRows = Array.Empty<Domain.Models.ReceiptReportRecord>();

    public ReceiptReportsForm(IReceiptReportService receiptReportService, IReceiptRdlcExportService receiptRdlcExportService)
    {
        this.receiptReportService = receiptReportService;
        this.receiptRdlcExportService = receiptRdlcExportService;

        Text = "Receipt Reports";
        Width = 1220;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;

        var panel = new Panel { Dock = DockStyle.Top, Height = 85 };

        modeComboBox = new ComboBox
        {
            Left = 20,
            Top = 25,
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        modeComboBox.Items.AddRange(new object[]
        {
            "Voucher No",
            "Ledger Name",
            "Date Range"
        });
        modeComboBox.SelectedIndex = 0;

        voucherNoTextBox = new TextBox { Left = 295, Top = 25, Width = 120, PlaceholderText = "No" };
        ledgerNameTextBox = new TextBox { Left = 430, Top = 25, Width = 220, PlaceholderText = "Ledger name" };
        fromDatePicker = new DateTimePicker { Left = 665, Top = 25, Width = 150, Format = DateTimePickerFormat.Short };
        toDatePicker = new DateTimePicker { Left = 825, Top = 25, Width = 150, Format = DateTimePickerFormat.Short };

        loadButton = new Button { Left = 990, Top = 23, Width = 120, Height = 30, Text = "Load" };
        loadButton.Click += async (_, _) => await LoadAsync();

        exportPdfButton = new Button { Left = 1120, Top = 23, Width = 90, Height = 30, Text = "Export" };
        exportPdfButton.Click += async (_, _) => await ExportPdfAsync();

        previewButton = new Button { Left = 990, Top = 23, Width = 120, Height = 30, Text = "Preview/Print" };
        previewButton.Click += async (_, _) => await PreviewPdfAsync();

        panel.Controls.Add(modeComboBox);
        panel.Controls.Add(voucherNoTextBox);
        panel.Controls.Add(ledgerNameTextBox);
        panel.Controls.Add(fromDatePicker);
        panel.Controls.Add(toDatePicker);
        panel.Controls.Add(loadButton);
        panel.Controls.Add(previewButton);
        panel.Controls.Add(exportPdfButton);

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
        Controls.Add(panel);
    }

    private async Task LoadAsync()
    {
        loadButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            IReadOnlyList<Domain.Models.ReceiptReportRecord> rows;
            switch (modeComboBox.SelectedIndex)
            {
                case 0:
                    if (!int.TryParse(voucherNoTextBox.Text, out var voucherNo))
                    {
                        MessageBox.Show("Enter valid voucher no.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    rows = await receiptReportService.GetByVoucherNoAsync(voucherNo);
                    break;
                case 1:
                    if (string.IsNullOrWhiteSpace(ledgerNameTextBox.Text))
                    {
                        MessageBox.Show("Enter ledger name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    rows = await receiptReportService.GetByLedgerNameAsync(ledgerNameTextBox.Text);
                    break;
                default:
                    rows = await receiptReportService.GetByDateRangeAsync(fromDatePicker.Value.Date, toDatePicker.Value.Date);
                    break;
            }

            currentRows = rows;
            reportGrid.DataSource = currentRows.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to load receipt report: {exception.Message}", "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private async Task ExportPdfAsync()
    {
        if (currentRows.Count == 0)
        {
            MessageBox.Show("Load report data before exporting.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            FileName = $"receipt-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf",
            Title = "Export Receipt Report"
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
                0 => "Receipt Report - Voucher",
                1 => "Receipt Report - Ledger",
                _ => "Receipt Report - Date Range"
            };

            var pdfBytes = await receiptRdlcExportService.ExportPdfAsync(currentRows, title);
            await File.WriteAllBytesAsync(dialog.FileName, pdfBytes);

            MessageBox.Show("PDF exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to export PDF: {exception.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            exportPdfButton.Enabled = true;
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
                0 => "Receipt Report - Voucher",
                1 => "Receipt Report - Ledger",
                _ => "Receipt Report - Date Range"
            };

            var pdfBytes = await receiptRdlcExportService.ExportPdfAsync(currentRows, title);
            var tempPath = PdfPreviewHelper.SaveToTemporaryPdf(pdfBytes, "receipt-report-preview");
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
}