using DoctorSoft.Domain.Contracts;

namespace DoctorSoft.App.Forms;

public sealed class PatientHistoryReportsForm : Form
{
    private readonly IPatientHistoryReportService reportService;
    private readonly TextBox patientNameTextBox;
    private readonly Button loadButton;
    private readonly DataGridView reportGrid;

    public PatientHistoryReportsForm(IPatientHistoryReportService reportService)
    {
        this.reportService = reportService;

        Text = "Patient History Reports";
        Width = 1180;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        var panel = new Panel { Dock = DockStyle.Top, Height = 68 };

        patientNameTextBox = new TextBox
        {
            Left = 20,
            Top = 22,
            Width = 280,
            PlaceholderText = "Patient name"
        };

        loadButton = new Button
        {
            Left = 315,
            Top = 20,
            Width = 120,
            Height = 30,
            Text = "Load"
        };
        loadButton.Click += async (_, _) => await LoadAsync();

        panel.Controls.Add(patientNameTextBox);
        panel.Controls.Add(loadButton);

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
        if (string.IsNullOrWhiteSpace(patientNameTextBox.Text))
        {
            MessageBox.Show("Enter patient name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        loadButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var rows = await reportService.GetByPatientAsync(patientNameTextBox.Text);
            reportGrid.DataSource = rows.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to load patient history report: {exception.Message}", "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            UseWaitCursor = false;
        }
    }
}