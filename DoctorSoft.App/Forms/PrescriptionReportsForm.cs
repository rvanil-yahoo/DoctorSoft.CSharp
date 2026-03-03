using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class PrescriptionReportsForm : Form
{
    private readonly IPrescriptionReportService prescriptionReportService;
    private readonly ComboBox modeComboBox;
    private readonly TextBox prescIdTextBox;
    private readonly TextBox patientNameTextBox;
    private readonly DateTimePicker datePicker;
    private readonly Button loadButton;
    private readonly DataGridView reportGrid;

    public PrescriptionReportsForm(IPrescriptionReportService prescriptionReportService)
    {
        this.prescriptionReportService = prescriptionReportService;

        Text = "Prescription Reports";
        Width = 1250;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 78 };
        modeComboBox = new ComboBox
        {
            Left = 20,
            Top = 25,
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        modeComboBox.Items.AddRange(new object[]
        {
            "By Prescription Id (PrescriptionsRpt)",
            "By Patient (PatwisePresc)",
            "By Date (+ optional patient)"
        });
        modeComboBox.SelectedIndex = 0;

        prescIdTextBox = new TextBox { Left = 315, Top = 25, Width = 140, PlaceholderText = "Presc_Id" };
        patientNameTextBox = new TextBox { Left = 470, Top = 25, Width = 240, PlaceholderText = "Patient name" };
        datePicker = new DateTimePicker { Left = 725, Top = 25, Width = 160, Format = DateTimePickerFormat.Short };

        loadButton = new Button { Left = 900, Top = 23, Width = 120, Height = 30, Text = "Load" };
        loadButton.Click += async (_, _) => await LoadReportAsync();

        filterPanel.Controls.Add(modeComboBox);
        filterPanel.Controls.Add(prescIdTextBox);
        filterPanel.Controls.Add(patientNameTextBox);
        filterPanel.Controls.Add(datePicker);
        filterPanel.Controls.Add(loadButton);

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
    }

    private async Task LoadReportAsync()
    {
        loadButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var mode = modeComboBox.SelectedIndex;
            IReadOnlyList<PrescriptionReportRecord> rows;

            if (mode == 0)
            {
                if (!int.TryParse(prescIdTextBox.Text.Trim(), out var prescId))
                {
                    MessageBox.Show("Enter a valid Presc_Id.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                rows = await prescriptionReportService.GetByPrescriptionIdAsync(prescId);
            }
            else if (mode == 1)
            {
                if (string.IsNullOrWhiteSpace(patientNameTextBox.Text))
                {
                    MessageBox.Show("Enter patient name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                rows = await prescriptionReportService.GetByPatientAsync(patientNameTextBox.Text);
            }
            else
            {
                rows = await prescriptionReportService.GetByDateAsync(datePicker.Value.Date, patientNameTextBox.Text);
            }

            reportGrid.DataSource = rows.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to load prescription report data: {exception.Message}", "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            UseWaitCursor = false;
        }
    }
}
