using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class ReceiptSetupForm : Form
{
    private readonly IReceiptNameRepository receiptNameRepository;
    private readonly TextBox nameTextBox;
    private readonly CheckBox recPatCheckBox;
    private readonly DataGridView namesGrid;
    private readonly BindingSource namesBinding;
    private string? selectedOriginalName;

    public ReceiptSetupForm(IReceiptNameRepository receiptNameRepository)
    {
        this.receiptNameRepository = receiptNameRepository;

        Text = "Receipts Setup";
        Width = 820;
        Height = 670;
        StartPosition = FormStartPosition.CenterParent;

        nameTextBox = new TextBox { Left = 20, Top = 20, Width = 380, PlaceholderText = "Receipt Name" };
        recPatCheckBox = new CheckBox { Left = 420, Top = 22, Width = 180, Text = "Requires Patient" };

        var addButton = new Button { Left = 20, Top = 58, Width = 90, Height = 30, Text = "Add" };
        addButton.Click += async (_, _) => await AddAsync();

        var modifyButton = new Button { Left = 120, Top = 58, Width = 90, Height = 30, Text = "Modify" };
        modifyButton.Click += async (_, _) => await ModifyAsync();

        var deleteButton = new Button { Left = 220, Top = 58, Width = 90, Height = 30, Text = "Delete" };
        deleteButton.Click += async (_, _) => await DeleteAsync();

        namesBinding = new BindingSource();
        namesGrid = new DataGridView
        {
            Left = 20,
            Top = 100,
            Width = 760,
            Height = 530,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            DataSource = namesBinding,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        namesGrid.CellClick += (_, args) => SelectRow(args.RowIndex);

        Controls.Add(nameTextBox);
        Controls.Add(recPatCheckBox);
        Controls.Add(addButton);
        Controls.Add(modifyButton);
        Controls.Add(deleteButton);
        Controls.Add(namesGrid);

        Shown += async (_, _) => await RefreshGridAsync();
    }

    private async Task AddAsync()
    {
        var name = nameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter receipt name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (await receiptNameRepository.ExistsAsync(name))
        {
            MessageBox.Show("Receipt name already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await receiptNameRepository.AddAsync(name, recPatCheckBox.Checked);
        await RefreshGridAsync();
        ClearEditor();
    }

    private async Task ModifyAsync()
    {
        if (string.IsNullOrWhiteSpace(selectedOriginalName))
        {
            MessageBox.Show("Select a receipt name to modify.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var newName = nameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            MessageBox.Show("Enter new receipt name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!string.Equals(selectedOriginalName, newName, StringComparison.OrdinalIgnoreCase) && await receiptNameRepository.ExistsAsync(newName))
        {
            MessageBox.Show("Receipt name already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await receiptNameRepository.UpdateAsync(selectedOriginalName, newName, recPatCheckBox.Checked);
        await RefreshGridAsync();
        ClearEditor();
    }

    private async Task DeleteAsync()
    {
        if (string.IsNullOrWhiteSpace(selectedOriginalName))
        {
            MessageBox.Show("Select a receipt name to delete.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show("Delete selected receipt name?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        await receiptNameRepository.DeleteAsync(selectedOriginalName);
        await RefreshGridAsync();
        ClearEditor();
    }

    private async Task RefreshGridAsync()
    {
        var rows = await receiptNameRepository.GetAllAsync();
        namesBinding.DataSource = rows.ToList();
        namesBinding.ResetBindings(false);
    }

    private void SelectRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= namesBinding.Count)
        {
            return;
        }

        if (namesBinding[rowIndex] is not ReceiptName row)
        {
            return;
        }

        selectedOriginalName = row.Name;
        nameTextBox.Text = row.Name;
        recPatCheckBox.Checked = row.RequiresPatientSelection;
    }

    private void ClearEditor()
    {
        selectedOriginalName = null;
        nameTextBox.Clear();
        recPatCheckBox.Checked = false;
    }
}