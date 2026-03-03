using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class ReceiptVoucherForm : Form
{
    private readonly IReceiptVoucherRepository receiptVoucherRepository;
    private readonly IReceiptNameRepository receiptNameRepository;
    private readonly IPatientRepository patientRepository;

    private readonly Label voucherNoLabel;
    private readonly DateTimePicker voucherDatePicker;
    private readonly ComboBox ledgerComboBox;
    private readonly TextBox receiverTextBox;
    private readonly ComboBox patientComboBox;
    private readonly TextBox amountTextBox;

    private int currentVoucherNo;

    public ReceiptVoucherForm(
        IReceiptVoucherRepository receiptVoucherRepository,
        IReceiptNameRepository receiptNameRepository,
        IPatientRepository patientRepository)
    {
        this.receiptVoucherRepository = receiptVoucherRepository;
        this.receiptNameRepository = receiptNameRepository;
        this.patientRepository = patientRepository;

        Text = "Receipt Voucher";
        Width = 840;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;

        voucherNoLabel = new Label { Left = 20, Top = 20, Width = 260, Text = "Receipt No: -" };
        voucherDatePicker = new DateTimePicker { Left = 300, Top = 15, Width = 160, Format = DateTimePickerFormat.Short };

        ledgerComboBox = new ComboBox { Left = 20, Top = 60, Width = 420, DropDownStyle = ComboBoxStyle.DropDownList };
        ledgerComboBox.SelectedIndexChanged += async (_, _) => await ApplyReceiverModeAsync();

        receiverTextBox = new TextBox { Left = 20, Top = 100, Width = 420, PlaceholderText = "Receiver Name" };
        patientComboBox = new ComboBox { Left = 20, Top = 100, Width = 420, DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };

        amountTextBox = new TextBox { Left = 20, Top = 140, Width = 220, PlaceholderText = "Amount Received" };

        var saveButton = new Button { Left = 20, Top = 185, Width = 160, Height = 34, Text = "Receipt Given" };
        saveButton.Click += async (_, _) => await SaveAsync();

        Controls.Add(voucherNoLabel);
        Controls.Add(voucherDatePicker);
        Controls.Add(ledgerComboBox);
        Controls.Add(receiverTextBox);
        Controls.Add(patientComboBox);
        Controls.Add(amountTextBox);
        Controls.Add(saveButton);

        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await RefreshVoucherNoAsync();
        voucherDatePicker.Value = DateTime.Today;

        var ledgers = await receiptNameRepository.GetAllAsync();
        ledgerComboBox.DataSource = ledgers.ToList();
        ledgerComboBox.DisplayMember = nameof(ReceiptName.Name);
        ledgerComboBox.ValueMember = nameof(ReceiptName.Name);

        var patients = await patientRepository.SearchAsync(null, null, null);
        patientComboBox.DataSource = patients.ToList();
        patientComboBox.DisplayMember = nameof(Patient.Name);
        patientComboBox.ValueMember = nameof(Patient.Name);

        await ApplyReceiverModeAsync();
    }

    private async Task ApplyReceiverModeAsync()
    {
        if (ledgerComboBox.SelectedItem is not ReceiptName selected)
        {
            return;
        }

        var ledger = await receiptNameRepository.GetByNameAsync(selected.Name) ?? selected;
        patientComboBox.Visible = ledger.RequiresPatientSelection;
        receiverTextBox.Visible = !ledger.RequiresPatientSelection;
    }

    private async Task RefreshVoucherNoAsync()
    {
        currentVoucherNo = await receiptVoucherRepository.GetNextVoucherNoAsync();
        voucherNoLabel.Text = $"Receipt No: {currentVoucherNo}";
    }

    private async Task SaveAsync()
    {
        if (ledgerComboBox.SelectedItem is not ReceiptName ledger)
        {
            MessageBox.Show("Select ledger/receipt item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string receiverName;
        if (patientComboBox.Visible)
        {
            receiverName = (patientComboBox.SelectedItem as Patient)?.Name ?? string.Empty;
        }
        else
        {
            receiverName = receiverTextBox.Text.Trim();
        }

        if (string.IsNullOrWhiteSpace(receiverName))
        {
            MessageBox.Show("Enter/select receiver name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(amountTextBox.Text, out var amount) || amount <= 0)
        {
            MessageBox.Show("Enter valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var voucher = new ReceiptVoucher
        {
            VoucherNo = currentVoucherNo,
            VoucherDate = voucherDatePicker.Value.Date,
            ReceiverName = receiverName,
            LedgerName = ledger.Name,
            AmountReceived = amount
        };

        await receiptVoucherRepository.AddAsync(voucher);
        MessageBox.Show("Receipt voucher saved.", "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);

        amountTextBox.Clear();
        receiverTextBox.Clear();
        voucherDatePicker.Value = DateTime.Today;
        await RefreshVoucherNoAsync();
    }
}