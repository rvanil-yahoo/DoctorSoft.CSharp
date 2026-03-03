using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class PatientHistoryManagementForm : Form
{
    private readonly IPatientRepository patientRepository;
    private readonly IPatientHistoryRepository historyRepository;

    private readonly ComboBox patientComboBox;
    private readonly DateTimePicker testDatePicker;
    private readonly TextBox testNameTextBox;
    private readonly TextBox testDescriptionTextBox;
    private readonly TextBox observationsTextBox;

    private readonly TextBox patientFilterTextBox;
    private readonly CheckBox dateFilterCheckBox;
    private readonly DateTimePicker dateFilterPicker;
    private readonly DataGridView historyGrid;
    private readonly BindingSource historyBinding;

    private string? originalPatientName;
    private DateTime? originalTestDate;

    public PatientHistoryManagementForm(IPatientRepository patientRepository, IPatientHistoryRepository historyRepository)
    {
        this.patientRepository = patientRepository;
        this.historyRepository = historyRepository;

        Text = "Patient History Management";
        Width = 1260;
        Height = 820;
        StartPosition = FormStartPosition.CenterParent;

        var root = new Panel { Dock = DockStyle.Fill };

        patientComboBox = new ComboBox { Left = 20, Top = 20, Width = 270, DropDownStyle = ComboBoxStyle.DropDownList };
        testDatePicker = new DateTimePicker { Left = 305, Top = 20, Width = 160, Format = DateTimePickerFormat.Short };

        testNameTextBox = new TextBox { Left = 20, Top = 60, Width = 445, PlaceholderText = "Test Name" };
        testDescriptionTextBox = new TextBox { Left = 20, Top = 100, Width = 680, Height = 90, Multiline = true, PlaceholderText = "Test Description" };
        observationsTextBox = new TextBox { Left = 20, Top = 200, Width = 680, Height = 90, Multiline = true, PlaceholderText = "Observations" };

        var addButton = new Button { Left = 730, Top = 60, Width = 170, Height = 34, Text = "Add History" };
        addButton.Click += async (_, _) => await AddAsync();

        var updateButton = new Button { Left = 730, Top = 105, Width = 170, Height = 34, Text = "Update Selected" };
        updateButton.Click += async (_, _) => await UpdateAsync();

        var deleteButton = new Button { Left = 730, Top = 150, Width = 170, Height = 34, Text = "Delete Selected" };
        deleteButton.Click += async (_, _) => await DeleteAsync();

        var clearButton = new Button { Left = 730, Top = 195, Width = 170, Height = 34, Text = "Clear" };
        clearButton.Click += (_, _) => ClearEditor();

        patientFilterTextBox = new TextBox { Left = 20, Top = 320, Width = 260, PlaceholderText = "Filter by Patient Name" };
        dateFilterCheckBox = new CheckBox { Left = 295, Top = 323, Width = 120, Text = "Filter by Date" };
        dateFilterPicker = new DateTimePicker { Left = 420, Top = 320, Width = 160, Format = DateTimePickerFormat.Short, Enabled = false };
        dateFilterCheckBox.CheckedChanged += (_, _) => dateFilterPicker.Enabled = dateFilterCheckBox.Checked;

        var searchButton = new Button { Left = 595, Top = 318, Width = 110, Height = 30, Text = "Search" };
        searchButton.Click += async (_, _) => await RefreshGridAsync();

        var refreshButton = new Button { Left = 720, Top = 318, Width = 110, Height = 30, Text = "Refresh All" };
        refreshButton.Click += async (_, _) => await RefreshGridAsync(string.Empty, null);

        historyBinding = new BindingSource();
        historyGrid = new DataGridView
        {
            Left = 20,
            Top = 360,
            Width = 1200,
            Height = 420,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            DataSource = historyBinding,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        historyGrid.CellClick += (_, args) => LoadSelectedRow(args.RowIndex);

        root.Controls.AddRange(new Control[]
        {
            patientComboBox, testDatePicker, testNameTextBox, testDescriptionTextBox, observationsTextBox,
            addButton, updateButton, deleteButton, clearButton,
            patientFilterTextBox, dateFilterCheckBox, dateFilterPicker, searchButton, refreshButton,
            historyGrid
        });

        Controls.Add(root);
        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadPatientsAsync();
        testDatePicker.Value = DateTime.Today;
        await RefreshGridAsync();
    }

    private async Task LoadPatientsAsync()
    {
        var patients = await patientRepository.SearchAsync(null, null, null);
        patientComboBox.DataSource = patients.ToList();
        patientComboBox.DisplayMember = nameof(Patient.Name);
        patientComboBox.ValueMember = nameof(Patient.Name);
    }

    private async Task AddAsync()
    {
        var entry = BuildFromEditor();
        if (entry is null)
        {
            return;
        }

        var exists = await historyRepository.ExistsAsync(entry.PatientName, entry.TestDate);
        if (exists)
        {
            MessageBox.Show("History already exists for this patient and date.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await historyRepository.AddAsync(entry);
        MessageBox.Show("History added.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        SelectByKey(entry.PatientName, entry.TestDate);
    }

    private async Task UpdateAsync()
    {
        if (originalPatientName is null || !originalTestDate.HasValue)
        {
            MessageBox.Show("Select a row to update.", "History", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var entry = BuildFromEditor();
        if (entry is null)
        {
            return;
        }

        if (!string.Equals(entry.PatientName, originalPatientName, StringComparison.OrdinalIgnoreCase) || entry.TestDate.Date != originalTestDate.Value.Date)
        {
            var exists = await historyRepository.ExistsAsync(entry.PatientName, entry.TestDate);
            if (exists)
            {
                MessageBox.Show("Another history row already exists for this patient and date.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        await historyRepository.UpdateAsync(entry, originalPatientName, originalTestDate.Value);
        MessageBox.Show("History updated.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        SelectByKey(entry.PatientName, entry.TestDate);
    }

    private async Task DeleteAsync()
    {
        if (originalPatientName is null || !originalTestDate.HasValue)
        {
            MessageBox.Show("Select a row to delete.", "History", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show("Are you sure you want to delete selected history?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await historyRepository.DeleteAsync(originalPatientName, originalTestDate.Value);
        MessageBox.Show("History deleted.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        ClearEditor();
    }

    private async Task RefreshGridAsync(string? patientFilter = null, DateTime? dateFilter = null)
    {
        var patientName = patientFilter ?? patientFilterTextBox.Text;
        var testDate = dateFilter;
        if (dateFilter is null && dateFilterCheckBox.Checked)
        {
            testDate = dateFilterPicker.Value.Date;
        }

        var rows = await historyRepository.SearchAsync(patientName, testDate);
        historyBinding.DataSource = rows.ToList();
        historyBinding.ResetBindings(false);
    }

    private void LoadSelectedRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= historyBinding.Count)
        {
            return;
        }

        if (historyBinding[rowIndex] is not PatientHistoryEntry entry)
        {
            return;
        }

        PopulateEditor(entry);
        originalPatientName = entry.PatientName;
        originalTestDate = entry.TestDate.Date;
    }

    private void SelectByKey(string patientName, DateTime testDate)
    {
        for (var index = 0; index < historyBinding.Count; index++)
        {
            if (historyBinding[index] is not PatientHistoryEntry entry)
            {
                continue;
            }

            if (string.Equals(entry.PatientName, patientName, StringComparison.OrdinalIgnoreCase) && entry.TestDate.Date == testDate.Date)
            {
                historyGrid.ClearSelection();
                historyGrid.Rows[index].Selected = true;
                historyGrid.CurrentCell = historyGrid.Rows[index].Cells[0];
                LoadSelectedRow(index);
                return;
            }
        }
    }

    private PatientHistoryEntry? BuildFromEditor()
    {
        var patientName = patientComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(patientName))
        {
            MessageBox.Show("Patient is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        if (string.IsNullOrWhiteSpace(testNameTextBox.Text))
        {
            MessageBox.Show("Test name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        return new PatientHistoryEntry
        {
            PatientName = patientName,
            TestDate = testDatePicker.Value.Date,
            TestName = testNameTextBox.Text.Trim(),
            TestDescription = testDescriptionTextBox.Text.Trim(),
            Observations = observationsTextBox.Text.Trim()
        };
    }

    private void PopulateEditor(PatientHistoryEntry entry)
    {
        patientComboBox.Text = entry.PatientName;
        testDatePicker.Value = entry.TestDate == default ? DateTime.Today : entry.TestDate;
        testNameTextBox.Text = entry.TestName;
        testDescriptionTextBox.Text = entry.TestDescription;
        observationsTextBox.Text = entry.Observations;
    }

    private void ClearEditor()
    {
        testDatePicker.Value = DateTime.Today;
        testNameTextBox.Clear();
        testDescriptionTextBox.Clear();
        observationsTextBox.Clear();
        originalPatientName = null;
        originalTestDate = null;
    }
}