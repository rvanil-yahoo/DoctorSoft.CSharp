using DoctorSoft.Domain.Contracts;

namespace DoctorSoft.App.Forms;

public sealed class ObservationReportsForm : Form
{
    private readonly IObservationReportService observationReportService;
    private readonly ComboBox modeComboBox;
    private readonly DateTimePicker datePicker;
    private readonly TextBox patientNameTextBox;
    private readonly Button loadButton;
    private readonly DataGridView reportGrid;

    public ObservationReportsForm(IObservationReportService observationReportService)
    {
        this.observationReportService = observationReportService;

        Text = "Observation Reports";
        Width = 1220;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 72 };

        modeComboBox = new ComboBox
        {
            Left = 20,
            Top = 24,
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        modeComboBox.Items.AddRange(new object[]
        {
            "Date + patient (legacy patobservations)",
            "Date only",
            "Patient only"
        });
        modeComboBox.SelectedIndex = 0;

        datePicker = new DateTimePicker
        {
            Left = 315,
            Top = 24,
            Width = 160,
            Format = DateTimePickerFormat.Short
        };

        patientNameTextBox = new TextBox
        {
            Left = 490,
            Top = 24,
            Width = 220,
            PlaceholderText = "Patient name"
        };

        loadButton = new Button
        {
            Left = 725,
            Top = 22,
            Width = 120,
            Height = 30,
            Text = "Load"
        };
        loadButton.Click += async (_, _) => await LoadReportAsync();

        filterPanel.Controls.Add(modeComboBox);
        filterPanel.Controls.Add(datePicker);
        filterPanel.Controls.Add(patientNameTextBox);
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

        Shown += async (_, _) => await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        loadButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var mode = modeComboBox.SelectedIndex;
            IReadOnlyList<Domain.Models.ObservationReportRecord> rows = mode switch
            {
                0 => await LoadDateAndPatientAsync(),
                1 => await observationReportService.GetByDateAsync(datePicker.Value.Date),
                _ => await LoadByPatientAsync()
            };

            reportGrid.DataSource = rows.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to load observation report data: {exception.Message}", "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private async Task<IReadOnlyList<Domain.Models.ObservationReportRecord>> LoadDateAndPatientAsync()
    {
        if (string.IsNullOrWhiteSpace(patientNameTextBox.Text))
        {
            MessageBox.Show("Enter patient name for Date + patient mode.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return Array.Empty<Domain.Models.ObservationReportRecord>();
        }

        return await observationReportService.GetByDateAndPatientAsync(datePicker.Value.Date, patientNameTextBox.Text);
    }

    private async Task<IReadOnlyList<Domain.Models.ObservationReportRecord>> LoadByPatientAsync()
    {
        if (string.IsNullOrWhiteSpace(patientNameTextBox.Text))
        {
            MessageBox.Show("Enter patient name for Patient only mode.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return Array.Empty<Domain.Models.ObservationReportRecord>();
        }

        return await observationReportService.GetByPatientAsync(patientNameTextBox.Text);
    }
}