using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class AppointmentManagementForm : Form
{
    private readonly IAppointmentRepository appointmentRepository;
    private readonly IPatientRepository patientRepository;

    private readonly DateTimePicker filterDatePicker;
    private readonly CheckBox pendingOnlyCheckBox;
    private readonly TextBox filterPatientTextBox;
    private readonly DataGridView appointmentsGrid;

    private readonly DateTimePicker startDatePicker;
    private readonly ComboBox patientComboBox;
    private readonly TextBox patientAddressTextBox;
    private readonly TextBox patientAgeTextBox;
    private readonly TextBox patientSexTextBox;
    private readonly TextBox appTimeTextBox;
    private readonly TextBox titleTextBox;
    private readonly TextBox detailsTextBox;

    private readonly Button searchButton;
    private readonly Button addButton;
    private readonly Button completeButton;

    public AppointmentManagementForm(IAppointmentRepository appointmentRepository, IPatientRepository patientRepository)
    {
        this.appointmentRepository = appointmentRepository;
        this.patientRepository = patientRepository;

        Text = "Appointment Management";
        Width = 1200;
        Height = 760;
        StartPosition = FormStartPosition.CenterParent;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 360
        };

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 72 };
        filterDatePicker = new DateTimePicker { Left = 20, Top = 26, Width = 170, Format = DateTimePickerFormat.Short };
        pendingOnlyCheckBox = new CheckBox { Left = 205, Top = 29, Width = 120, Text = "Pending only", Checked = true };
        filterPatientTextBox = new TextBox { Left = 335, Top = 26, Width = 220, PlaceholderText = "Patient name contains" };
        searchButton = new Button { Left = 570, Top = 24, Width = 120, Height = 30, Text = "Search" };
        searchButton.Click += async (_, _) => await LoadAppointmentsAsync();

        topPanel.Controls.AddRange(new Control[] { filterDatePicker, pendingOnlyCheckBox, filterPatientTextBox, searchButton });

        appointmentsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        split.Panel1.Controls.Add(appointmentsGrid);
        split.Panel1.Controls.Add(topPanel);

        var detailsPanel = new Panel { Dock = DockStyle.Fill };
        startDatePicker = new DateTimePicker { Left = 20, Top = 35, Width = 170, Format = DateTimePickerFormat.Short };
        patientComboBox = new ComboBox { Left = 205, Top = 35, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
        patientComboBox.SelectedIndexChanged += async (_, _) => await PopulatePatientDetailsAsync();

        patientAddressTextBox = CreateReadOnly(detailsPanel, "Address", 470, 15, 300);
        patientAgeTextBox = CreateReadOnly(detailsPanel, "Age", 785, 15, 70);
        patientSexTextBox = CreateReadOnly(detailsPanel, "Sex", 870, 15, 70);

        appTimeTextBox = CreateEditable(detailsPanel, "App Time", 20, 95, 120);
        titleTextBox = CreateEditable(detailsPanel, "Title", 155, 95, 300);
        detailsTextBox = CreateEditable(detailsPanel, "Details", 470, 95, 470);

        addButton = new Button { Left = 20, Top = 165, Width = 160, Height = 34, Text = "Add Appointment" };
        completeButton = new Button { Left = 195, Top = 165, Width = 180, Height = 34, Text = "Mark Selected Complete" };

        addButton.Click += async (_, _) => await AddAppointmentAsync();
        completeButton.Click += async (_, _) => await CompleteSelectedAsync();

        detailsPanel.Controls.Add(new Label { Left = 20, Top = 15, Width = 170, Text = "Start Date" });
        detailsPanel.Controls.Add(startDatePicker);
        detailsPanel.Controls.Add(new Label { Left = 205, Top = 15, Width = 250, Text = "Patient" });
        detailsPanel.Controls.Add(patientComboBox);
        detailsPanel.Controls.Add(addButton);
        detailsPanel.Controls.Add(completeButton);

        split.Panel2.Controls.Add(detailsPanel);
        Controls.Add(split);

        Shown += async (_, _) =>
        {
            await LoadPatientsAsync();
            await LoadAppointmentsAsync();
        };
    }

    private TextBox CreateReadOnly(Control container, string labelText, int left, int top, int width)
    {
        var label = new Label { Left = left, Top = top, Width = width, Text = labelText };
        var textbox = new TextBox { Left = left, Top = top + 20, Width = width, ReadOnly = true };
        container.Controls.Add(label);
        container.Controls.Add(textbox);
        return textbox;
    }

    private TextBox CreateEditable(Control container, string labelText, int left, int top, int width)
    {
        var label = new Label { Left = left, Top = top, Width = width, Text = labelText };
        var textbox = new TextBox { Left = left, Top = top + 20, Width = width };
        container.Controls.Add(label);
        container.Controls.Add(textbox);
        return textbox;
    }

    private async Task LoadPatientsAsync()
    {
        var patients = await patientRepository.SearchAsync(null, null, null);
        patientComboBox.DataSource = patients.ToList();
        patientComboBox.DisplayMember = nameof(Patient.Name);
        patientComboBox.ValueMember = nameof(Patient.Name);
    }

    private async Task PopulatePatientDetailsAsync()
    {
        if (patientComboBox.SelectedItem is not Patient selected)
        {
            return;
        }

        var patient = await patientRepository.GetByNameAsync(selected.Name) ?? selected;
        patientAddressTextBox.Text = patient.Address;
        patientAgeTextBox.Text = patient.Age?.ToString() ?? string.Empty;
        patientSexTextBox.Text = patient.Sex;
    }

    private async Task LoadAppointmentsAsync()
    {
        ToggleBusy(true);
        try
        {
            var rows = await appointmentRepository.SearchAsync(filterDatePicker.Value.Date, pendingOnlyCheckBox.Checked, filterPatientTextBox.Text);
            appointmentsGrid.DataSource = rows.ToList();
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private async Task AddAppointmentAsync()
    {
        if (patientComboBox.SelectedItem is not Patient selectedPatient)
        {
            MessageBox.Show("Select a patient.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(appTimeTextBox.Text))
        {
            MessageBox.Show("Appointment time is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(titleTextBox.Text))
        {
            MessageBox.Show("Appointment title is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ToggleBusy(true);
        try
        {
            var exists = await appointmentRepository.ExistsAsync(startDatePicker.Value.Date, selectedPatient.Name);
            if (exists)
            {
                MessageBox.Show("Appointment already exists for this patient on this date.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var appointment = new Appointment
            {
                DateAdded = DateTime.Now,
                StartDate = startDatePicker.Value.Date,
                EventTitle = titleTextBox.Text.Trim(),
                EventDetails = detailsTextBox.Text.Trim(),
                AppTime = appTimeTextBox.Text.Trim(),
                PatientName = selectedPatient.Name,
                PatientAddress = selectedPatient.Address,
                PatientAge = selectedPatient.Age,
                PatientSex = selectedPatient.Sex,
                Status = false
            };

            await appointmentRepository.AddAsync(appointment);
            MessageBox.Show("Appointment added.", "Appointment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadAppointmentsAsync();
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private async Task CompleteSelectedAsync()
    {
        if (appointmentsGrid.CurrentRow?.DataBoundItem is not Appointment selected)
        {
            MessageBox.Show("Select an appointment from the list.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show("Mark selected appointment as completed?", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        ToggleBusy(true);
        try
        {
            await appointmentRepository.MarkCompletedAsync(selected.StartDate, selected.PatientName, selected.AppTime);
            await LoadAppointmentsAsync();
            MessageBox.Show("Appointment marked completed.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void ToggleBusy(bool busy)
    {
        searchButton.Enabled = !busy;
        addButton.Enabled = !busy;
        completeButton.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
