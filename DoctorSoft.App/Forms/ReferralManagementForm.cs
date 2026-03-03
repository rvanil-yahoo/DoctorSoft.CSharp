using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class ReferralManagementForm : Form
{
    private readonly IPatientRepository patientRepository;
    private readonly IReferralRepository referralRepository;

    private readonly ComboBox patientComboBox;
    private readonly DateTimePicker refDatePicker;
    private readonly TextBox patientAddressTextBox;
    private readonly TextBox patientAgeTextBox;
    private readonly TextBox patientSexTextBox;
    private readonly TextBox fromDoctorTextBox;
    private readonly TextBox fromClinicTextBox;
    private readonly TextBox fromClinicAddressTextBox;
    private readonly TextBox toDoctorTextBox;
    private readonly TextBox toClinicTextBox;
    private readonly TextBox toAddressTextBox;
    private readonly TextBox messageTextBox;

    private readonly TextBox patientFilterTextBox;
    private readonly TextBox toDoctorFilterTextBox;
    private readonly DataGridView referralsGrid;
    private readonly BindingSource referralsBinding;

    private string? originalPatientName;
    private DateTime? originalRefDate;

    public ReferralManagementForm(IPatientRepository patientRepository, IReferralRepository referralRepository)
    {
        this.patientRepository = patientRepository;
        this.referralRepository = referralRepository;

        Text = "Referral Management";
        Width = 1320;
        Height = 840;
        StartPosition = FormStartPosition.CenterParent;

        var root = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var topLabel = new Label
        {
            Left = 20,
            Top = 14,
            Width = 500,
            Text = "Legacy Refferral CRUD (Add / Modify / Delete / Find)",
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        patientComboBox = new ComboBox { Left = 20, Top = 50, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
        var draftButton = new Button { Left = 295, Top = 49, Width = 140, Height = 30, Text = "Load Draft" };
        draftButton.Click += async (_, _) => await LoadDraftAsync();

        refDatePicker = new DateTimePicker { Left = 450, Top = 50, Width = 150, Format = DateTimePickerFormat.Short };

        patientAddressTextBox = new TextBox { Left = 20, Top = 95, Width = 290, PlaceholderText = "Patient Address" };
        patientAgeTextBox = new TextBox { Left = 325, Top = 95, Width = 80, PlaceholderText = "Age" };
        patientSexTextBox = new TextBox { Left = 420, Top = 95, Width = 90, PlaceholderText = "Sex" };

        fromDoctorTextBox = new TextBox { Left = 20, Top = 140, Width = 240, PlaceholderText = "From Doctor" };
        fromClinicTextBox = new TextBox { Left = 275, Top = 140, Width = 240, PlaceholderText = "From Clinic" };
        fromClinicAddressTextBox = new TextBox { Left = 530, Top = 140, Width = 280, PlaceholderText = "From Clinic Address" };

        toDoctorTextBox = new TextBox { Left = 20, Top = 185, Width = 240, PlaceholderText = "To Doctor" };
        toClinicTextBox = new TextBox { Left = 275, Top = 185, Width = 240, PlaceholderText = "To Clinic" };
        toAddressTextBox = new TextBox { Left = 530, Top = 185, Width = 280, PlaceholderText = "To Address" };

        messageTextBox = new TextBox { Left = 20, Top = 230, Width = 790, Height = 70, Multiline = true, PlaceholderText = "Message" };

        var addButton = new Button { Left = 830, Top = 92, Width = 150, Height = 34, Text = "Add Referral" };
        addButton.Click += async (_, _) => await AddAsync();

        var updateButton = new Button { Left = 830, Top = 137, Width = 150, Height = 34, Text = "Update Selected" };
        updateButton.Click += async (_, _) => await UpdateAsync();

        var deleteButton = new Button { Left = 830, Top = 182, Width = 150, Height = 34, Text = "Delete Selected" };
        deleteButton.Click += async (_, _) => await DeleteAsync();

        var clearButton = new Button { Left = 830, Top = 227, Width = 150, Height = 34, Text = "Clear" };
        clearButton.Click += (_, _) => ClearEditor();

        patientFilterTextBox = new TextBox { Left = 20, Top = 325, Width = 260, PlaceholderText = "Filter by Patient Name" };
        toDoctorFilterTextBox = new TextBox { Left = 295, Top = 325, Width = 240, PlaceholderText = "Filter by To Doctor" };

        var searchButton = new Button { Left = 550, Top = 323, Width = 120, Height = 30, Text = "Search" };
        searchButton.Click += async (_, _) => await RefreshGridAsync();

        var refreshButton = new Button { Left = 685, Top = 323, Width = 120, Height = 30, Text = "Refresh All" };
        refreshButton.Click += async (_, _) => await RefreshGridAsync(string.Empty, string.Empty);

        referralsBinding = new BindingSource();
        referralsGrid = new DataGridView
        {
            Left = 20,
            Top = 365,
            Width = 1240,
            Height = 420,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            DataSource = referralsBinding,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        referralsGrid.CellClick += (_, args) => LoadSelectedRow(args.RowIndex);

        root.Controls.AddRange(new Control[]
        {
            topLabel,
            patientComboBox, draftButton, refDatePicker,
            patientAddressTextBox, patientAgeTextBox, patientSexTextBox,
            fromDoctorTextBox, fromClinicTextBox, fromClinicAddressTextBox,
            toDoctorTextBox, toClinicTextBox, toAddressTextBox,
            messageTextBox,
            addButton, updateButton, deleteButton, clearButton,
            patientFilterTextBox, toDoctorFilterTextBox, searchButton, refreshButton,
            referralsGrid
        });

        Controls.Add(root);

        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadPatientsAsync();
        refDatePicker.Value = DateTime.Today;
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
            MessageBox.Show("Select patient.", "Referral", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var draft = await referralRepository.BuildDraftForPatientAsync(selected.Name);
        if (draft is null)
        {
            MessageBox.Show("Patient details not found.", "Referral", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PopulateEditor(draft);
        originalPatientName = null;
        originalRefDate = null;
    }

    private async Task AddAsync()
    {
        var referral = BuildFromEditor();
        if (referral is null)
        {
            return;
        }

        var exists = await referralRepository.ExistsAsync(referral.PatientName, referral.RefDate);
        if (exists)
        {
            MessageBox.Show("Referral already exists for this patient and date.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await referralRepository.AddAsync(referral);
        MessageBox.Show("Referral added.", "Referral", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        SelectByKey(referral.PatientName, referral.RefDate);
    }

    private async Task UpdateAsync()
    {
        if (originalPatientName is null || !originalRefDate.HasValue)
        {
            MessageBox.Show("Select a referral row to update.", "Referral", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var referral = BuildFromEditor();
        if (referral is null)
        {
            return;
        }

        if (!string.Equals(referral.PatientName, originalPatientName, StringComparison.OrdinalIgnoreCase) || referral.RefDate.Date != originalRefDate.Value.Date)
        {
            var exists = await referralRepository.ExistsAsync(referral.PatientName, referral.RefDate);
            if (exists)
            {
                MessageBox.Show("Another referral already exists for this patient and date.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        await referralRepository.UpdateAsync(referral, originalPatientName, originalRefDate.Value);
        MessageBox.Show("Referral updated.", "Referral", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        SelectByKey(referral.PatientName, referral.RefDate);
    }

    private async Task DeleteAsync()
    {
        if (originalPatientName is null || !originalRefDate.HasValue)
        {
            MessageBox.Show("Select a referral row to delete.", "Referral", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show("Are you sure you want to delete the selected referral?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await referralRepository.DeleteAsync(originalPatientName, originalRefDate.Value);
        MessageBox.Show("Referral deleted.", "Referral", MessageBoxButtons.OK, MessageBoxIcon.Information);

        await RefreshGridAsync();
        ClearEditor();
    }

    private async Task RefreshGridAsync(string? patientNameFilter = null, string? toDoctorFilter = null)
    {
        var patientFilter = patientNameFilter ?? patientFilterTextBox.Text;
        var toDoctorFilterValue = toDoctorFilter ?? toDoctorFilterTextBox.Text;

        var rows = await referralRepository.SearchAsync(patientFilter, toDoctorFilterValue);
        referralsBinding.DataSource = rows.ToList();
        referralsBinding.ResetBindings(false);
    }

    private void LoadSelectedRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= referralsBinding.Count)
        {
            return;
        }

        if (referralsBinding[rowIndex] is not Referral referral)
        {
            return;
        }

        PopulateEditor(referral);
        originalPatientName = referral.PatientName;
        originalRefDate = referral.RefDate.Date;
    }

    private void SelectByKey(string patientName, DateTime refDate)
    {
        for (var index = 0; index < referralsBinding.Count; index++)
        {
            if (referralsBinding[index] is not Referral referral)
            {
                continue;
            }

            if (string.Equals(referral.PatientName, patientName, StringComparison.OrdinalIgnoreCase) &&
                referral.RefDate.Date == refDate.Date)
            {
                referralsGrid.ClearSelection();
                referralsGrid.Rows[index].Selected = true;
                referralsGrid.CurrentCell = referralsGrid.Rows[index].Cells[0];
                LoadSelectedRow(index);
                return;
            }
        }
    }

    private void PopulateEditor(Referral referral)
    {
        patientComboBox.Text = referral.PatientName;
        refDatePicker.Value = referral.RefDate == default ? DateTime.Today : referral.RefDate;
        patientAddressTextBox.Text = referral.PatientAddress;
        patientAgeTextBox.Text = referral.PatientAge?.ToString() ?? string.Empty;
        patientSexTextBox.Text = referral.PatientSex;
        fromDoctorTextBox.Text = referral.FromDoctor;
        fromClinicTextBox.Text = referral.FromClinic;
        fromClinicAddressTextBox.Text = referral.FromClinicAddress;
        toDoctorTextBox.Text = referral.ToDoctor;
        toClinicTextBox.Text = referral.ToClinic;
        toAddressTextBox.Text = referral.ToAddress;
        messageTextBox.Text = referral.Message;
    }

    private Referral? BuildFromEditor()
    {
        var patientName = patientComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(patientName) || string.IsNullOrWhiteSpace(toDoctorTextBox.Text))
        {
            MessageBox.Show("Patient name and To Doctor are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        return new Referral
        {
            RefDate = refDatePicker.Value.Date,
            PatientName = patientName,
            PatientAddress = patientAddressTextBox.Text.Trim(),
            PatientAge = int.TryParse(patientAgeTextBox.Text, out var age) ? age : null,
            PatientSex = patientSexTextBox.Text.Trim(),
            FromDoctor = fromDoctorTextBox.Text.Trim(),
            FromClinic = fromClinicTextBox.Text.Trim(),
            FromClinicAddress = fromClinicAddressTextBox.Text.Trim(),
            ToDoctor = toDoctorTextBox.Text.Trim(),
            ToClinic = toClinicTextBox.Text.Trim(),
            ToAddress = toAddressTextBox.Text.Trim(),
            Message = messageTextBox.Text.Trim()
        };
    }

    private void ClearEditor()
    {
        refDatePicker.Value = DateTime.Today;
        patientAddressTextBox.Clear();
        patientAgeTextBox.Clear();
        patientSexTextBox.Clear();
        fromDoctorTextBox.Clear();
        fromClinicTextBox.Clear();
        fromClinicAddressTextBox.Clear();
        toDoctorTextBox.Clear();
        toClinicTextBox.Clear();
        toAddressTextBox.Clear();
        messageTextBox.Clear();
        originalPatientName = null;
        originalRefDate = null;
    }
}