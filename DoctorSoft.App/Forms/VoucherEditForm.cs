using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class VoucherEditForm : Form
{
    private readonly DateTimePicker voucherDatePicker;
    private readonly TextBox primaryTextBox;
    private readonly TextBox secondaryTextBox;
    private readonly TextBox tertiaryTextBox;
    private readonly TextBox amountTextBox;
    private readonly TextBox narrationTextBox;

    public PaymentMaintenanceRecord? PaymentRecord { get; private set; }
    public ReceiptMaintenanceRecord? ReceiptRecord { get; private set; }

    public VoucherEditForm(PaymentMaintenanceRecord source)
    {
        Text = $"Edit Payment Voucher #{source.VoucherNo}";
        Width = 560;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        voucherDatePicker = new DateTimePicker
        {
            Left = 170,
            Top = 22,
            Width = 150,
            Format = DateTimePickerFormat.Short,
            Value = source.VoucherDate == DateTime.MinValue ? DateTime.Today : source.VoucherDate
        };

        primaryTextBox = new TextBox { Left = 170, Top = 58, Width = 330, Text = source.PaidTowards };
        secondaryTextBox = new TextBox { Left = 170, Top = 94, Width = 330, Text = source.ReceiverName };
        tertiaryTextBox = new TextBox { Left = 170, Top = 130, Width = 330, Text = source.PaidBy };
        amountTextBox = new TextBox { Left = 170, Top = 166, Width = 150, Text = source.AmountPaid.ToString("0.##") };
        narrationTextBox = new TextBox { Left = 170, Top = 202, Width = 330, Text = source.ExpenditureCause };

        Controls.Add(new Label { Left = 20, Top = 26, Width = 130, Text = "Voucher Date" });
        Controls.Add(new Label { Left = 20, Top = 62, Width = 130, Text = "Paid Towards" });
        Controls.Add(new Label { Left = 20, Top = 98, Width = 130, Text = "Receiver Name" });
        Controls.Add(new Label { Left = 20, Top = 134, Width = 130, Text = "Paid By" });
        Controls.Add(new Label { Left = 20, Top = 170, Width = 130, Text = "Amount" });
        Controls.Add(new Label { Left = 20, Top = 206, Width = 130, Text = "Expenditure Cause" });

        Controls.Add(voucherDatePicker);
        Controls.Add(primaryTextBox);
        Controls.Add(secondaryTextBox);
        Controls.Add(tertiaryTextBox);
        Controls.Add(amountTextBox);
        Controls.Add(narrationTextBox);

        var saveButton = new Button { Left = 300, Top = 252, Width = 95, Height = 32, Text = "Save" };
        saveButton.Click += (_, _) => SavePayment(source.VoucherNo);

        var cancelButton = new Button { Left = 405, Top = 252, Width = 95, Height = 32, Text = "Cancel" };
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.Add(saveButton);
        Controls.Add(cancelButton);
    }

    public VoucherEditForm(ReceiptMaintenanceRecord source)
    {
        Text = $"Edit Receipt Voucher #{source.VoucherNo}";
        Width = 560;
        Height = 320;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        voucherDatePicker = new DateTimePicker
        {
            Left = 170,
            Top = 22,
            Width = 150,
            Format = DateTimePickerFormat.Short,
            Value = source.VoucherDate == DateTime.MinValue ? DateTime.Today : source.VoucherDate
        };

        primaryTextBox = new TextBox { Left = 170, Top = 58, Width = 330, Text = source.ReceiverName };
        secondaryTextBox = new TextBox { Left = 170, Top = 94, Width = 330, Text = source.LedgerName };
        tertiaryTextBox = new TextBox { Left = 170, Top = 130, Width = 1, Visible = false };
        amountTextBox = new TextBox { Left = 170, Top = 130, Width = 150, Text = source.AmountReceived.ToString("0.##") };
        narrationTextBox = new TextBox { Left = 170, Top = 166, Width = 1, Visible = false };

        Controls.Add(new Label { Left = 20, Top = 26, Width = 130, Text = "Voucher Date" });
        Controls.Add(new Label { Left = 20, Top = 62, Width = 130, Text = "Receiver Name" });
        Controls.Add(new Label { Left = 20, Top = 98, Width = 130, Text = "Ledger Name" });
        Controls.Add(new Label { Left = 20, Top = 134, Width = 130, Text = "Amount" });

        Controls.Add(voucherDatePicker);
        Controls.Add(primaryTextBox);
        Controls.Add(secondaryTextBox);
        Controls.Add(amountTextBox);

        var saveButton = new Button { Left = 300, Top = 214, Width = 95, Height = 32, Text = "Save" };
        saveButton.Click += (_, _) => SaveReceipt(source.VoucherNo);

        var cancelButton = new Button { Left = 405, Top = 214, Width = 95, Height = 32, Text = "Cancel" };
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.Add(saveButton);
        Controls.Add(cancelButton);
    }

    private void SavePayment(int voucherNo)
    {
        if (!decimal.TryParse(amountTextBox.Text, out var amount) || amount <= 0)
        {
            MessageBox.Show("Enter valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(primaryTextBox.Text) || string.IsNullOrWhiteSpace(secondaryTextBox.Text))
        {
            MessageBox.Show("Paid towards and receiver name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PaymentRecord = new PaymentMaintenanceRecord
        {
            VoucherNo = voucherNo,
            VoucherDate = voucherDatePicker.Value.Date,
            PaidTowards = primaryTextBox.Text.Trim(),
            ReceiverName = secondaryTextBox.Text.Trim(),
            PaidBy = tertiaryTextBox.Text.Trim(),
            AmountPaid = amount,
            ExpenditureCause = narrationTextBox.Text.Trim()
        };

        DialogResult = DialogResult.OK;
    }

    private void SaveReceipt(int voucherNo)
    {
        if (!decimal.TryParse(amountTextBox.Text, out var amount) || amount <= 0)
        {
            MessageBox.Show("Enter valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(primaryTextBox.Text) || string.IsNullOrWhiteSpace(secondaryTextBox.Text))
        {
            MessageBox.Show("Receiver name and ledger name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ReceiptRecord = new ReceiptMaintenanceRecord
        {
            VoucherNo = voucherNo,
            VoucherDate = voucherDatePicker.Value.Date,
            ReceiverName = primaryTextBox.Text.Trim(),
            LedgerName = secondaryTextBox.Text.Trim(),
            AmountReceived = amount
        };

        DialogResult = DialogResult.OK;
    }
}
