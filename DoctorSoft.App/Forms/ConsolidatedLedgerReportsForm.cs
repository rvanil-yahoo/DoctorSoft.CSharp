using DoctorSoft.Domain.Contracts;
using DoctorSoft.App.Utilities;

namespace DoctorSoft.App.Forms;

public sealed class ConsolidatedLedgerReportsForm : Form
{
    private readonly IConsolidatedLedgerReportService reportService;
    private readonly IConsolidatedLedgerRdlcExportService consolidatedLedgerRdlcExportService;
    private readonly ComboBox modeComboBox;
    private readonly TextBox ledgerNameTextBox;
    private readonly DateTimePicker fromDatePicker;
    private readonly DateTimePicker toDatePicker;
    private readonly Button loadButton;
    private readonly Button previewButton;
    private readonly Button exportPdfButton;
    private readonly DataGridView reportGrid;
    private readonly Label totalsLabel;
    private IReadOnlyList<Domain.Models.ConsolidatedLedgerRecord> currentRows = Array.Empty<Domain.Models.ConsolidatedLedgerRecord>();

    public ConsolidatedLedgerReportsForm(IConsolidatedLedgerReportService reportService, IConsolidatedLedgerRdlcExportService consolidatedLedgerRdlcExportService)
    {
        this.reportService = reportService;
        this.consolidatedLedgerRdlcExportService = consolidatedLedgerRdlcExportService;

        Text = "Consolidated Ledger Report";
        Width = 1300;
        Height = 760;
        StartPosition = FormStartPosition.CenterParent;

        var panel = new Panel { Dock = DockStyle.Top, Height = 90 };

        modeComboBox = new ComboBox
        {
            Left = 20,
            Top = 18,
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        modeComboBox.Items.AddRange(new object[]
        {
            "Date Range (Consolidated)",
            "Date Range + Ledger"
        });
        modeComboBox.SelectedIndex = 0;

        fromDatePicker = new DateTimePicker { Left = 315, Top = 18, Width = 150, Format = DateTimePickerFormat.Short };
        toDatePicker = new DateTimePicker { Left = 475, Top = 18, Width = 150, Format = DateTimePickerFormat.Short };
        ledgerNameTextBox = new TextBox { Left = 635, Top = 18, Width = 220, PlaceholderText = "Ledger Name" };

        loadButton = new Button { Left = 870, Top = 16, Width = 120, Height = 30, Text = "Load" };
        loadButton.Click += async (_, _) => await LoadAsync();

        exportPdfButton = new Button { Left = 1000, Top = 16, Width = 120, Height = 30, Text = "Export PDF" };
        exportPdfButton.Click += async (_, _) => await ExportPdfAsync();

        previewButton = new Button { Left = 1130, Top = 16, Width = 120, Height = 30, Text = "Preview/Print" };
        previewButton.Click += async (_, _) => await PreviewPdfAsync();

        totalsLabel = new Label
        {
            Left = 20,
            Top = 55,
            Width = 1100,
            Height = 24,
            Text = "Totals:"
        };

        panel.Controls.Add(modeComboBox);
        panel.Controls.Add(fromDatePicker);
        panel.Controls.Add(toDatePicker);
        panel.Controls.Add(ledgerNameTextBox);
        panel.Controls.Add(loadButton);
        panel.Controls.Add(exportPdfButton);
        panel.Controls.Add(previewButton);
        panel.Controls.Add(totalsLabel);

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
            IReadOnlyList<Domain.Models.ConsolidatedLedgerRecord> rows;
            if (modeComboBox.SelectedIndex == 1)
            {
                if (string.IsNullOrWhiteSpace(ledgerNameTextBox.Text))
                {
                    MessageBox.Show("Enter ledger name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                rows = await reportService.GetByDateRangeAndLedgerAsync(fromDatePicker.Value.Date, toDatePicker.Value.Date, ledgerNameTextBox.Text);
            }
            else
            {
                rows = await reportService.GetByDateRangeAsync(fromDatePicker.Value.Date, toDatePicker.Value.Date);
            }

            currentRows = rows;
            reportGrid.DataSource = currentRows.ToList();

            var totalDebit = rows.Sum(x => x.Debit);
            var totalCredit = rows.Sum(x => x.Credit);
            var finalBalance = rows.Count > 0 ? rows[^1].RunningBalance : 0m;
            totalsLabel.Text = $"Totals: Debit={totalDebit:N2}  Credit={totalCredit:N2}  Balance={finalBalance:N2}";
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to load consolidated ledger report: {exception.Message}", "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show("Load report data before exporting.", "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            FileName = $"consolidated-ledger-{DateTime.Now:yyyyMMdd-HHmmss}.pdf",
            Title = "Export Consolidated Ledger"
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
            var title = modeComboBox.SelectedIndex == 1
                ? "Consolidated Ledger - Date + Ledger"
                : "Consolidated Ledger - Date Range";

            var pdfBytes = await consolidatedLedgerRdlcExportService.ExportPdfAsync(currentRows, title);
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
            var title = modeComboBox.SelectedIndex == 1
                ? "Consolidated Ledger - Date + Ledger"
                : "Consolidated Ledger - Date Range";

            var pdfBytes = await consolidatedLedgerRdlcExportService.ExportPdfAsync(currentRows, title);
            var tempPath = PdfPreviewHelper.SaveToTemporaryPdf(pdfBytes, "consolidated-ledger-preview");
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