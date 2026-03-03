using DoctorSoft.Domain.Contracts;
using DoctorSoft.App.Utilities;

namespace DoctorSoft.App.Forms;

public sealed class PaymentReportsForm : Form
{
    private readonly IPaymentReportService paymentReportService;
    private readonly IPaymentRdlcExportService paymentRdlcExportService;
    private readonly ComboBox modeComboBox;
    private readonly TextBox voucherNoTextBox;
    private readonly TextBox paymentNameTextBox;
    private readonly DateTimePicker fromDatePicker;
    private readonly DateTimePicker toDatePicker;
    private readonly Button loadButton;
    private readonly Button previewButton;
    private readonly Button exportPdfButton;
    private readonly DataGridView reportGrid;
    private IReadOnlyList<Domain.Models.PaymentReportRecord> currentRows = Array.Empty<Domain.Models.PaymentReportRecord>();

    public PaymentReportsForm(IPaymentReportService paymentReportService, IPaymentRdlcExportService paymentRdlcExportService)
    {
        this.paymentReportService = paymentReportService;
        this.paymentRdlcExportService = paymentRdlcExportService;

        Text = "Payment Reports";
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
            "Payment Name",
            "Date Range"
        });
        modeComboBox.SelectedIndex = 0;

        voucherNoTextBox = new TextBox { Left = 295, Top = 25, Width = 120, PlaceholderText = "No" };
        paymentNameTextBox = new TextBox { Left = 430, Top = 25, Width = 220, PlaceholderText = "Payment name" };
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
        panel.Controls.Add(paymentNameTextBox);
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
            IReadOnlyList<Domain.Models.PaymentReportRecord> rows;
            switch (modeComboBox.SelectedIndex)
            {
                case 0:
                    if (!int.TryParse(voucherNoTextBox.Text, out var voucherNo))
                    {
                        MessageBox.Show("Enter valid voucher no.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    rows = await paymentReportService.GetByVoucherNoAsync(voucherNo);
                    break;
                case 1:
                    if (string.IsNullOrWhiteSpace(paymentNameTextBox.Text))
                    {
                        MessageBox.Show("Enter payment name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    rows = await paymentReportService.GetByPaymentNameAsync(paymentNameTextBox.Text);
                    break;
                default:
                    rows = await paymentReportService.GetByDateRangeAsync(fromDatePicker.Value.Date, toDatePicker.Value.Date);
                    break;
            }

            currentRows = rows;
            reportGrid.DataSource = currentRows.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to load payment report: {exception.Message}", "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            FileName = $"payment-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf",
            Title = "Export Payment Report"
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
                0 => "Payment Report - Voucher",
                1 => "Payment Report - Name",
                _ => "Payment Report - Date Range"
            };

            var pdfBytes = await paymentRdlcExportService.ExportPdfAsync(currentRows, title);
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
                0 => "Payment Report - Voucher",
                1 => "Payment Report - Name",
                _ => "Payment Report - Date Range"
            };

            var pdfBytes = await paymentRdlcExportService.ExportPdfAsync(currentRows, title);
            var tempPath = PdfPreviewHelper.SaveToTemporaryPdf(pdfBytes, "payment-report-preview");
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