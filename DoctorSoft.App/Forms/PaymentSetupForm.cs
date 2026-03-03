using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class PaymentSetupForm : Form
{
    private readonly IPaymentNameRepository paymentNameRepository;
    private readonly TextBox nameTextBox;
    private readonly DataGridView namesGrid;
    private readonly BindingSource namesBinding;
    private string? selectedOriginalName;

    public PaymentSetupForm(IPaymentNameRepository paymentNameRepository)
    {
        this.paymentNameRepository = paymentNameRepository;

        Text = "Payments Setup";
        Width = 760;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        nameTextBox = new TextBox { Left = 20, Top = 20, Width = 420, PlaceholderText = "Payment Name" };

        var addButton = new Button { Left = 460, Top = 18, Width = 90, Height = 30, Text = "Add" };
        addButton.Click += async (_, _) => await AddAsync();

        var modifyButton = new Button { Left = 560, Top = 18, Width = 90, Height = 30, Text = "Modify" };
        modifyButton.Click += async (_, _) => await ModifyAsync();

        var deleteButton = new Button { Left = 660, Top = 18, Width = 70, Height = 30, Text = "Delete" };
        deleteButton.Click += async (_, _) => await DeleteAsync();

        namesBinding = new BindingSource();
        namesGrid = new DataGridView
        {
            Left = 20,
            Top = 65,
            Width = 710,
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
            MessageBox.Show("Enter payment name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (await paymentNameRepository.ExistsAsync(name))
        {
            MessageBox.Show("Payment name already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await paymentNameRepository.AddAsync(name);
        await RefreshGridAsync();
        nameTextBox.Clear();
    }

    private async Task ModifyAsync()
    {
        if (string.IsNullOrWhiteSpace(selectedOriginalName))
        {
            MessageBox.Show("Select a payment name to modify.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var newName = nameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            MessageBox.Show("Enter new name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!string.Equals(selectedOriginalName, newName, StringComparison.OrdinalIgnoreCase) && await paymentNameRepository.ExistsAsync(newName))
        {
            MessageBox.Show("Payment name already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await paymentNameRepository.RenameAsync(selectedOriginalName, newName);
        await RefreshGridAsync();
        selectedOriginalName = null;
        nameTextBox.Clear();
    }

    private async Task DeleteAsync()
    {
        if (string.IsNullOrWhiteSpace(selectedOriginalName))
        {
            MessageBox.Show("Select a payment name to delete.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show("Delete selected payment name?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        await paymentNameRepository.DeleteAsync(selectedOriginalName);
        await RefreshGridAsync();
        selectedOriginalName = null;
        nameTextBox.Clear();
    }

    private async Task RefreshGridAsync()
    {
        var rows = await paymentNameRepository.GetAllAsync();
        namesBinding.DataSource = rows.ToList();
        namesBinding.ResetBindings(false);
    }

    private void SelectRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= namesBinding.Count)
        {
            return;
        }

        if (namesBinding[rowIndex] is not PaymentName row)
        {
            return;
        }

        selectedOriginalName = row.Name;
        nameTextBox.Text = row.Name;
    }
}