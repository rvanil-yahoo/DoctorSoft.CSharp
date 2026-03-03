using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class PatientManagementForm : Form
{
    private readonly IPatientRepository patientRepository;
    private readonly TextBox searchNameTextBox;
    private readonly TextBox searchAddressTextBox;
    private readonly TextBox searchBloodGroupTextBox;
    private readonly DataGridView resultsGrid;
    private readonly TextBox nameTextBox;
    private readonly TextBox addressTextBox;
    private readonly TextBox phoneTextBox;
    private readonly TextBox ageTextBox;
    private readonly TextBox sexTextBox;
    private readonly TextBox bloodGroupTextBox;
    private readonly Button searchButton;
    private readonly Button newButton;
    private readonly Button saveButton;

    private string? selectedOriginalName;

    public PatientManagementForm(IPatientRepository patientRepository)
    {
        this.patientRepository = patientRepository;

        Text = "Patient Management";
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 360
        };

        var searchPanel = new Panel { Dock = DockStyle.Top, Height = 65 };
        searchNameTextBox = new TextBox { Left = 20, Top = 25, Width = 220, PlaceholderText = "Search by name" };
        searchAddressTextBox = new TextBox { Left = 255, Top = 25, Width = 260, PlaceholderText = "Search by address" };
        searchBloodGroupTextBox = new TextBox { Left = 530, Top = 25, Width = 120, PlaceholderText = "Blood group" };
        searchButton = new Button { Left = 665, Top = 23, Width = 120, Height = 30, Text = "Search" };
        searchButton.Click += async (_, _) => await LoadResultsAsync();

        searchPanel.Controls.AddRange(new Control[] { searchNameTextBox, searchAddressTextBox, searchBloodGroupTextBox, searchButton });

        resultsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        resultsGrid.SelectionChanged += ResultsGrid_SelectionChanged;

        split.Panel1.Controls.Add(resultsGrid);
        split.Panel1.Controls.Add(searchPanel);

        var detailsPanel = new Panel { Dock = DockStyle.Fill };
        nameTextBox = CreateLabeledTextBox(detailsPanel, "Name", 20, 25, 300);
        addressTextBox = CreateLabeledTextBox(detailsPanel, "Address", 20, 85, 500);
        phoneTextBox = CreateLabeledTextBox(detailsPanel, "Phone", 20, 145, 250);
        ageTextBox = CreateLabeledTextBox(detailsPanel, "Age", 290, 145, 100);
        sexTextBox = CreateLabeledTextBox(detailsPanel, "Sex", 410, 145, 110);
        bloodGroupTextBox = CreateLabeledTextBox(detailsPanel, "Blood Group", 540, 145, 120);

        newButton = new Button { Left = 20, Top = 215, Width = 120, Height = 34, Text = "New" };
        saveButton = new Button { Left = 155, Top = 215, Width = 120, Height = 34, Text = "Save" };
        newButton.Click += (_, _) => ResetEditor();
        saveButton.Click += async (_, _) => await SaveAsync();

        detailsPanel.Controls.Add(newButton);
        detailsPanel.Controls.Add(saveButton);

        split.Panel2.Controls.Add(detailsPanel);
        Controls.Add(split);

        Shown += async (_, _) => await LoadResultsAsync();
    }

    private static TextBox CreateLabeledTextBox(Control container, string labelText, int left, int top, int width)
    {
        var label = new Label { Left = left, Top = top, Width = width, Text = labelText };
        var textBox = new TextBox { Left = left, Top = top + 20, Width = width };
        container.Controls.Add(label);
        container.Controls.Add(textBox);
        return textBox;
    }

    private async Task LoadResultsAsync()
    {
        ToggleBusy(true);
        try
        {
            var rows = await patientRepository.SearchAsync(searchNameTextBox.Text, searchAddressTextBox.Text, searchBloodGroupTextBox.Text);
            resultsGrid.DataSource = rows.ToList();
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void ResultsGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (resultsGrid.CurrentRow?.DataBoundItem is not Patient patient)
        {
            return;
        }

        selectedOriginalName = patient.Name;
        nameTextBox.Text = patient.Name;
        addressTextBox.Text = patient.Address;
        phoneTextBox.Text = patient.Phone;
        ageTextBox.Text = patient.Age?.ToString() ?? string.Empty;
        sexTextBox.Text = patient.Sex;
        bloodGroupTextBox.Text = patient.BloodGroup;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
        {
            MessageBox.Show("Patient name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var patient = new Patient
        {
            Name = nameTextBox.Text.Trim(),
            Address = addressTextBox.Text.Trim(),
            Phone = phoneTextBox.Text.Trim(),
            Age = int.TryParse(ageTextBox.Text.Trim(), out var age) ? age : null,
            Sex = sexTextBox.Text.Trim(),
            BloodGroup = bloodGroupTextBox.Text.Trim()
        };

        ToggleBusy(true);
        try
        {
            if (string.IsNullOrWhiteSpace(selectedOriginalName))
            {
                await patientRepository.AddAsync(patient);
                MessageBox.Show("Patient added.", "Patient", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                await patientRepository.UpdateAsync(patient, selectedOriginalName);
                MessageBox.Show("Patient updated.", "Patient", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            await LoadResultsAsync();
            selectedOriginalName = patient.Name;
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to save patient: {exception.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void ResetEditor()
    {
        selectedOriginalName = null;
        nameTextBox.Text = string.Empty;
        addressTextBox.Text = string.Empty;
        phoneTextBox.Text = string.Empty;
        ageTextBox.Text = string.Empty;
        sexTextBox.Text = string.Empty;
        bloodGroupTextBox.Text = string.Empty;
        nameTextBox.Focus();
    }

    private void ToggleBusy(bool busy)
    {
        searchButton.Enabled = !busy;
        saveButton.Enabled = !busy;
        newButton.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
