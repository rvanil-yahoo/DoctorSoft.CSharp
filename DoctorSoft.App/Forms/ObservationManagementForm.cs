using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class ObservationManagementForm : Form
{
    private readonly IPatientRepository patientRepository;
    private readonly IObservationRepository observationRepository;

    private readonly ComboBox patientComboBox;
    private readonly DateTimePicker datePicker;
    private readonly DateTimePicker timePicker;
    private readonly TextBox ageTextBox;
    private readonly TextBox sexTextBox;
    private readonly TextBox problemTextBox;
    private readonly TextBox observationTextBox;
    private readonly TextBox testsTextBox;

    private readonly TextBox patientFilterTextBox;
    private readonly DateTimePicker filterDatePicker;
    private readonly CheckBox filterByDateCheckBox;
    private readonly DataGridView observationsGrid;
    private readonly BindingSource observationsBinding;

    private string? originalPatientName;
    private DateTime? originalDate;

    public ObservationManagementForm(IPatientRepository patientRepository, IObservationRepository observationRepository)
    {
        this.patientRepository = patientRepository;
        this.observationRepository = observationRepository;

        Text = "Observation Management";
        Width = 1320;
        Height = 860;
        StartPosition = FormStartPosition.CenterParent;

        var root = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var title = new Label
        {
            Left = 20,
            Top = 14,
            Width = 500,
            Text = "Patient Observations (Add / Modify / Delete)",
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        patientComboBox = new ComboBox { Left = 20, Top = 50, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
        datePicker = new DateTimePicker { Left = 295, Top = 50, Width = 150, Format = DateTimePickerFormat.Short };
        timePicker = new DateTimePicker { Left = 460, Top = 50, Width = 150, Format = DateTimePickerFormat.Time, ShowUpDown = true };

        var loadDraftButton = new Button { Left = 625, Top = 49, Width = 140, Height = 30, Text = "Load Draft" };
        loadDraftButton.Click += async (_, _) => await LoadDraftAsync();

        ageTextBox = new TextBox { Left = 20, Top = 95, Width = 80, PlaceholderText = "Age" };
        sexTextBox = new TextBox { Left = 115, Top = 95, Width = 90, PlaceholderText = "Sex" };

        problemTextBox = new TextBox { Left = 20, Top = 140, Width = 790, Height = 70, Multiline = true, PlaceholderText = "Problem" };
        observationTextBox = new TextBox { Left = 20, Top = 220, Width = 790, Height = 80, Multiline = true, PlaceholderText = "Observation" };
        testsTextBox = new TextBox { Left = 20, Top = 310, Width = 790, Height = 70, Multiline = true, PlaceholderText = "Tests Recommended" };

        var addButton = new Button { Left = 830, Top = 138, Width = 160, Height = 34, Text = "Add Observation" };
        addButton.Click += async (_, _) => await AddAsync();

        var updateButton = new Button { Left = 830, Top = 183, Width = 160, Height = 34, Text = "Update Selected" };
        updateButton.Click += async (_, _) => await UpdateAsync();

        var deleteButton = new Button { Left = 830, Top = 228, Width = 160, Height = 34, Text = "Delete Selected" };
        deleteButton.Click += async (_, _) => await DeleteAsync();

        var clearButton = new Button { Left = 830, Top = 273, Width = 160, Height = 34, Text = "Clear" };
        clearButton.Click += (_, _) => ClearEditor();

        patientFilterTextBox = new TextBox { Left = 20, Top = 400, Width = 250, PlaceholderText = "Filter by Patient Name" };
        filterByDateCheckBox = new CheckBox { Left = 285, Top = 403, Width = 120, Text = "Filter by Date" };
        filterDatePicker = new DateTimePicker { Left = 410, Top = 400, Width = 150, Format = DateTimePickerFormat.Short, Enabled = false };
        filterByDateCheckBox.CheckedChanged += (_, _) => filterDatePicker.Enabled = filterByDateCheckBox.Checked;

        var searchButton = new Button { Left = 575, Top = 398, Width = 120, Height = 30, Text = "Search" };
        searchButton.Click += async (_, _) => await RefreshGridAsync();

        var refreshButton = new Button { Left = 710, Top = 398, Width = 120, Height = 30, Text = "Refresh All" };
        refreshButton.Click += async (_, _) => await RefreshGridAsync(string.Empty, null);

        observationsBinding = new BindingSource();
        observationsGrid = new DataGridView
        {
            Left = 20,
            Top = 440,
            Width = 1240,
            Height = 370,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            DataSource = observationsBinding,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        observationsGrid.CellClick += (_, args) => LoadSelectedRow(args.RowIndex);

        root.Controls.AddRange(new Control[]
        {
            title,
            patientComboBox, datePicker, timePicker, loadDraftButton,
            ageTextBox, sexTextBox,
            problemTextBox, observationTextBox, testsTextBox,
            addButton, updateButton, deleteButton, clearButton,
            patientFilterTextBox, filterByDateCheckBox, filterDatePicker, searchButton, refreshButton,
            observationsGrid
        });

        Controls.Add(root);

        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadPatientsAsync();
        datePicker.Value = DateTime.Today;
        timePicker.Value = DateTime.Now;
        await RefreshGridAsync();
    }

    private async Task LoadPatientsAsync()
    {
        var patients = await patientRepository.SearchAsync(null, null, null);
        patientComboBox.DataSource = patients.ToList();
        patientComboBox.DisplayMember = nameof(Patient.Name);
        patientComboBox.ValueMember = nameof(Patient.Name);
    }

    private async Task LoadDraftAsync()
    {
        if (patientComboBox.SelectedItem is not Patient selected)
        {
            MessageBox.Show("Select patient.", "Observation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var draft = await observationRepository.BuildDraftForPatientAsync(selected.Name);
        if (draft is null)
        {
            MessageBox.Show("Patient details not found.", "Observation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PopulateEditor(draft);
        originalPatientName = null;
        originalDate = null;
    }

    private async Task AddAsync()
    {
        var model = BuildFromEditor();
        if (model is null)
        {
            return;
        }

        var exists = await observationRepository.ExistsAsync(model.PatientName, model.Date);
        if (exists)
        {
            MessageBox.Show("Observation already exists for this patient and date.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await observationRepository.AddAsync(model);
        MessageBox.Show("Observation added.", "Observation", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        SelectByKey(model.PatientName, model.Date);
    }

    private async Task UpdateAsync()
    {
        if (originalPatientName is null || !originalDate.HasValue)
        {
            MessageBox.Show("Select a row to update.", "Observation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var model = BuildFromEditor();
        if (model is null)
        {
            return;
        }

        if (!string.Equals(model.PatientName, originalPatientName, StringComparison.OrdinalIgnoreCase) || model.Date.Date != originalDate.Value.Date)
        {
            var exists = await observationRepository.ExistsAsync(model.PatientName, model.Date);
            if (exists)
            {
                MessageBox.Show("Another observation already exists for this patient and date.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        await observationRepository.UpdateAsync(model, originalPatientName, originalDate.Value);
        MessageBox.Show("Observation updated.", "Observation", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        SelectByKey(model.PatientName, model.Date);
    }

    private async Task DeleteAsync()
    {
        if (originalPatientName is null || !originalDate.HasValue)
        {
            MessageBox.Show("Select a row to delete.", "Observation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show("Are you sure you want to delete the selected observation?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await observationRepository.DeleteAsync(originalPatientName, originalDate.Value);
        MessageBox.Show("Observation deleted.", "Observation", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        ClearEditor();
    }

    private async Task RefreshGridAsync(string? patientNameFilter = null, DateTime? dateFilter = null)
    {
        var filterName = patientNameFilter ?? patientFilterTextBox.Text;
        var filterDate = dateFilter;

        if (dateFilter is null && filterByDateCheckBox.Checked)
        {
            filterDate = filterDatePicker.Value.Date;
        }

        var list = await observationRepository.SearchAsync(filterName, filterDate);
        observationsBinding.DataSource = list.ToList();
        observationsBinding.ResetBindings(false);
    }

    private void LoadSelectedRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= observationsBinding.Count)
        {
            return;
        }

        if (observationsBinding[rowIndex] is not Observation model)
        {
            return;
        }

        PopulateEditor(model);
        originalPatientName = model.PatientName;
        originalDate = model.Date.Date;
    }

    private void SelectByKey(string patientName, DateTime date)
    {
        for (var index = 0; index < observationsBinding.Count; index++)
        {
            if (observationsBinding[index] is not Observation model)
            {
                continue;
            }

            if (string.Equals(model.PatientName, patientName, StringComparison.OrdinalIgnoreCase) &&
                model.Date.Date == date.Date)
            {
                observationsGrid.ClearSelection();
                observationsGrid.Rows[index].Selected = true;
                observationsGrid.CurrentCell = observationsGrid.Rows[index].Cells[0];
                LoadSelectedRow(index);
                return;
            }
        }
    }

    private Observation? BuildFromEditor()
    {
        var patientName = patientComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(patientName))
        {
            MessageBox.Show("Patient name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        if (string.IsNullOrWhiteSpace(problemTextBox.Text))
        {
            MessageBox.Show("Problem is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        if (string.IsNullOrWhiteSpace(observationTextBox.Text))
        {
            MessageBox.Show("Observation text is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        if (string.IsNullOrWhiteSpace(testsTextBox.Text))
        {
            MessageBox.Show("Tests recommended is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        return new Observation
        {
            Date = datePicker.Value.Date,
            Time = timePicker.Value.ToString("HH:mm:ss"),
            PatientName = patientName,
            Age = int.TryParse(ageTextBox.Text, out var age) ? age : null,
            Sex = sexTextBox.Text.Trim(),
            Problem = problemTextBox.Text.Trim(),
            ObservationText = observationTextBox.Text.Trim(),
            TestsRecommended = testsTextBox.Text.Trim()
        };
    }

    private void PopulateEditor(Observation model)
    {
        patientComboBox.Text = model.PatientName;
        datePicker.Value = model.Date == default ? DateTime.Today : model.Date;
        if (TimeSpan.TryParse(model.Time, out var parsedTime))
        {
            timePicker.Value = DateTime.Today.Add(parsedTime);
        }
        else
        {
            timePicker.Value = DateTime.Now;
        }

        ageTextBox.Text = model.Age?.ToString() ?? string.Empty;
        sexTextBox.Text = model.Sex;
        problemTextBox.Text = model.Problem;
        observationTextBox.Text = model.ObservationText;
        testsTextBox.Text = model.TestsRecommended;
    }

    private void ClearEditor()
    {
        datePicker.Value = DateTime.Today;
        timePicker.Value = DateTime.Now;
        ageTextBox.Clear();
        sexTextBox.Clear();
        problemTextBox.Clear();
        observationTextBox.Clear();
        testsTextBox.Clear();
        originalPatientName = null;
        originalDate = null;
    }
}