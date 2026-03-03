using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class PrescriptionManagementForm : Form
{
    private readonly IPatientRepository patientRepository;
    private readonly IMedicineRepository medicineRepository;
    private readonly IPrescriptionRepository prescriptionRepository;

    private readonly Label prescIdLabel;
    private readonly ComboBox patientComboBox;
    private readonly TextBox patientAddressTextBox;
    private readonly TextBox patientAgeTextBox;
    private readonly DateTimePicker datePicker;
    private readonly DateTimePicker timePicker;

    private readonly ComboBox medicineComboBox;
    private readonly TextBox medicineTypeTextBox;
    private readonly ComboBox dosageComboBox;
    private readonly ComboBox quantityComboBox;

    private readonly DataGridView linesGrid;
    private readonly BindingSource linesBinding;
    private readonly Button addMedicineButton;
    private readonly Button savePrescriptionButton;

    private int currentPrescriptionId;

    public PrescriptionManagementForm(
        IPatientRepository patientRepository,
        IMedicineRepository medicineRepository,
        IPrescriptionRepository prescriptionRepository)
    {
        this.patientRepository = patientRepository;
        this.medicineRepository = medicineRepository;
        this.prescriptionRepository = prescriptionRepository;

        Text = "Prescription Management";
        Width = 1200;
        Height = 760;
        StartPosition = FormStartPosition.CenterParent;

        var panel = new Panel { Dock = DockStyle.Fill };

        prescIdLabel = new Label { Left = 20, Top = 20, Width = 320, Text = "Prescription Id: -" };

        patientComboBox = new ComboBox { Left = 20, Top = 55, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
        patientComboBox.SelectedIndexChanged += async (_, _) => await PopulatePatientDetailsAsync();

        patientAddressTextBox = new TextBox { Left = 315, Top = 55, Width = 280, ReadOnly = true, PlaceholderText = "Patient Address" };
        patientAgeTextBox = new TextBox { Left = 610, Top = 55, Width = 100, ReadOnly = true, PlaceholderText = "Age" };

        datePicker = new DateTimePicker { Left = 725, Top = 55, Width = 160, Format = DateTimePickerFormat.Short };
        timePicker = new DateTimePicker { Left = 900, Top = 55, Width = 160, Format = DateTimePickerFormat.Time, ShowUpDown = true };

        medicineComboBox = new ComboBox { Left = 20, Top = 105, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
        medicineComboBox.SelectedIndexChanged += async (_, _) => await PopulateMedicineTypeAsync();

        medicineTypeTextBox = new TextBox { Left = 295, Top = 105, Width = 150, ReadOnly = true, PlaceholderText = "Type" };

        dosageComboBox = new ComboBox { Left = 460, Top = 105, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        dosageComboBox.Items.AddRange(new object[] { "1", "2", "3", "4", "5", "6" });

        quantityComboBox = new ComboBox { Left = 595, Top = 105, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        quantityComboBox.Items.AddRange(new object[]
        {
            "1/4 Tablet", "1/2 Tablet", "3/4 Tablet", "1 Tablet", "2 Tablets",
            "1/4 Teaspoon", "1/2 Teaspoon", "3/4 Teaspoon", "1 Teaspoon", "2 Teaspoons"
        });

        addMedicineButton = new Button { Left = 780, Top = 103, Width = 150, Height = 30, Text = "Add Medicine" };
        addMedicineButton.Click += (_, _) => AddMedicineLine();

        linesBinding = new BindingSource();
        linesGrid = new DataGridView
        {
            Left = 20,
            Top = 150,
            Width = 1120,
            Height = 460,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            DataSource = linesBinding
        };

        savePrescriptionButton = new Button { Left = 20, Top = 625, Width = 170, Height = 34, Text = "Save Prescription" };
        savePrescriptionButton.Click += async (_, _) => await SavePrescriptionAsync();

        panel.Controls.AddRange(new Control[]
        {
            prescIdLabel, patientComboBox, patientAddressTextBox, patientAgeTextBox, datePicker, timePicker,
            medicineComboBox, medicineTypeTextBox, dosageComboBox, quantityComboBox, addMedicineButton,
            linesGrid, savePrescriptionButton
        });

        Controls.Add(panel);

        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadPatientsAsync();
        await LoadMedicinesAsync();
        await RefreshPrescriptionIdAsync();

        linesBinding.DataSource = new List<PrescriptionLine>();
        dosageComboBox.SelectedIndex = 0;
        quantityComboBox.SelectedIndex = 0;
    }

    private async Task LoadPatientsAsync()
    {
        var patients = await patientRepository.SearchAsync(null, null, null);
        patientComboBox.DataSource = patients.ToList();
        patientComboBox.DisplayMember = nameof(Patient.Name);
        patientComboBox.ValueMember = nameof(Patient.Name);
    }

    private async Task LoadMedicinesAsync()
    {
        var medicines = await medicineRepository.GetAllAsync();
        medicineComboBox.DataSource = medicines.ToList();
        medicineComboBox.DisplayMember = nameof(MedicineInfo.Medicine);
        medicineComboBox.ValueMember = nameof(MedicineInfo.Medicine);
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
        datePicker.Value = DateTime.Today;
        timePicker.Value = DateTime.Now;
    }

    private async Task PopulateMedicineTypeAsync()
    {
        if (medicineComboBox.SelectedItem is not MedicineInfo selected)
        {
            return;
        }

        var medicine = await medicineRepository.GetByNameAsync(selected.Medicine) ?? selected;
        medicineTypeTextBox.Text = medicine.Type;
    }

    private void AddMedicineLine()
    {
        if (medicineComboBox.SelectedItem is not MedicineInfo selectedMedicine)
        {
            MessageBox.Show("Select a medicine.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(dosageComboBox.Text))
        {
            MessageBox.Show("Select dosage.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(quantityComboBox.Text))
        {
            MessageBox.Show("Select quantity.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rows = (List<PrescriptionLine>)linesBinding.DataSource!;
        rows.Add(new PrescriptionLine
        {
            PrescId = currentPrescriptionId,
            Medicine = selectedMedicine.Medicine,
            Type = medicineTypeTextBox.Text,
            Dosage = dosageComboBox.Text,
            Quantity = quantityComboBox.Text
        });

        linesBinding.ResetBindings(false);
    }

    private async Task SavePrescriptionAsync()
    {
        if (patientComboBox.SelectedItem is not Patient selectedPatient)
        {
            MessageBox.Show("Select patient.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lines = (List<PrescriptionLine>)linesBinding.DataSource!;
        if (lines.Count == 0)
        {
            MessageBox.Show("Add at least one medicine line.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var date = datePicker.Value.Date;
        var exists = await prescriptionRepository.ExistsForPatientAndDateAsync(selectedPatient.Name, date);
        if (exists)
        {
            MessageBox.Show("Prescription already exists for this patient on selected date.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var prescription = new Prescription
        {
            PrescId = currentPrescriptionId,
            PatientName = selectedPatient.Name,
            PatientAddress = patientAddressTextBox.Text,
            PatientAge = int.TryParse(patientAgeTextBox.Text, out var age) ? age : null,
            Date = date,
            Time = timePicker.Value.ToString("hh:mm:ss tt"),
            Lines = lines.Select(line => new PrescriptionLine
            {
                PrescId = currentPrescriptionId,
                Medicine = line.Medicine,
                Type = line.Type,
                Dosage = line.Dosage,
                Quantity = line.Quantity
            }).ToList()
        };

        await prescriptionRepository.SaveAsync(prescription);
        MessageBox.Show("Prescription saved.", "Prescription", MessageBoxButtons.OK, MessageBoxIcon.Information);

        linesBinding.DataSource = new List<PrescriptionLine>();
        linesBinding.ResetBindings(false);
        await RefreshPrescriptionIdAsync();
    }

    private async Task RefreshPrescriptionIdAsync()
    {
        currentPrescriptionId = await prescriptionRepository.GetNextPrescriptionIdAsync();
        prescIdLabel.Text = $"Prescription Id: {currentPrescriptionId}";
    }
}
