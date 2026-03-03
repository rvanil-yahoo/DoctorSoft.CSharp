using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class LedgerEntryEditForm : Form
{
    private readonly DateTimePicker datePicker;
    private readonly TextBox ledgerNameTextBox;
    private readonly ComboBox amountTypeComboBox;
    private readonly TextBox amountTextBox;
    private readonly TextBox narrationTextBox;

    private readonly LedgerMaintenanceRecord source;

    public LedgerMaintenanceRecord? UpdatedRecord { get; private set; }

    public LedgerEntryEditForm(LedgerMaintenanceRecord source)
    {
        this.source = source;

        Text = $"Edit Ledger Entry #{source.AutoId}";
        Width = 560;
        Height = 340;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(new Label { Left = 20, Top = 26, Width = 130, Text = "Date" });
        datePicker = new DateTimePicker
        {
            Left = 170,
            Top = 22,
            Width = 150,
            Format = DateTimePickerFormat.Short,
            Value = source.Date == DateTime.MinValue ? DateTime.Today : source.Date
        };
        Controls.Add(datePicker);

        Controls.Add(new Label { Left = 20, Top = 62, Width = 130, Text = "Ledger Name" });
        ledgerNameTextBox = new TextBox { Left = 170, Top = 58, Width = 340, Text = source.LedgerName };
        Controls.Add(ledgerNameTextBox);

        Controls.Add(new Label { Left = 20, Top = 98, Width = 130, Text = "Amount Type" });
        amountTypeComboBox = new ComboBox
        {
            Left = 170,
            Top = 94,
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        amountTypeComboBox.Items.AddRange(new object[] { "Debit", "Credit" });
        amountTypeComboBox.SelectedIndex = source.Credit > 0m ? 1 : 0;
        Controls.Add(amountTypeComboBox);

        Controls.Add(new Label { Left = 20, Top = 134, Width = 130, Text = "Amount" });
        var amount = source.Credit > 0m ? source.Credit : source.Debit;
        amountTextBox = new TextBox { Left = 170, Top = 130, Width = 140, Text = amount.ToString("0.##") };
        Controls.Add(amountTextBox);

        Controls.Add(new Label { Left = 20, Top = 170, Width = 130, Text = "Narration" });
        narrationTextBox = new TextBox { Left = 170, Top = 166, Width = 340, Text = source.Narration };
        Controls.Add(narrationTextBox);

        var saveButton = new Button { Left = 315, Top = 224, Width = 95, Height = 32, Text = "Save" };
        saveButton.Click += (_, _) => Save();
        Controls.Add(saveButton);

        var cancelButton = new Button { Left = 415, Top = 224, Width = 95, Height = 32, Text = "Cancel" };
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;
        Controls.Add(cancelButton);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(ledgerNameTextBox.Text))
        {
            MessageBox.Show("Ledger name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(amountTextBox.Text.Trim(), out var amount) || amount <= 0m)
        {
            MessageBox.Show("Enter valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var isCredit = amountTypeComboBox.SelectedIndex == 1;

        UpdatedRecord = new LedgerMaintenanceRecord
        {
            AutoId = source.AutoId,
            VoucherNo = source.VoucherNo,
            Date = datePicker.Value.Date,
            LedgerName = ledgerNameTextBox.Text.Trim(),
            Debit = isCredit ? 0m : amount,
            Credit = isCredit ? amount : 0m,
            Narration = narrationTextBox.Text.Trim()
        };

        DialogResult = DialogResult.OK;
    }
}
