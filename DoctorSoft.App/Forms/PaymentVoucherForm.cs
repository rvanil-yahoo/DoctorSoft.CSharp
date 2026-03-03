using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;

namespace DoctorSoft.App.Forms;

public sealed class PaymentVoucherForm : Form
{
    private readonly IPaymentVoucherRepository paymentVoucherRepository;
    private readonly IPaymentNameRepository paymentNameRepository;

    private readonly Label voucherNoLabel;
    private readonly DateTimePicker voucherDatePicker;
    private readonly ComboBox paidTowardsComboBox;
    private readonly TextBox receiverTextBox;
    private readonly TextBox paidByTextBox;
    private readonly TextBox amountTextBox;
    private readonly TextBox expenditureTextBox;

    private int currentVoucherNo;

    public PaymentVoucherForm(IPaymentVoucherRepository paymentVoucherRepository, IPaymentNameRepository paymentNameRepository)
    {
        this.paymentVoucherRepository = paymentVoucherRepository;
        this.paymentNameRepository = paymentNameRepository;

        Text = "Payment Voucher";
        Width = 840;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;

        voucherNoLabel = new Label { Left = 20, Top = 20, Width = 250, Text = "Voucher No: -" };
        voucherDatePicker = new DateTimePicker { Left = 280, Top = 15, Width = 160, Format = DateTimePickerFormat.Short };

        paidTowardsComboBox = new ComboBox { Left = 20, Top = 60, Width = 420, DropDownStyle = ComboBoxStyle.DropDownList };
        receiverTextBox = new TextBox { Left = 20, Top = 100, Width = 420, PlaceholderText = "Name of the Receiver" };
        paidByTextBox = new TextBox { Left = 20, Top = 140, Width = 420, PlaceholderText = "Paid By" };
        amountTextBox = new TextBox { Left = 20, Top = 180, Width = 220, PlaceholderText = "Amount Paid" };
        expenditureTextBox = new TextBox { Left = 20, Top = 220, Width = 780, Height = 220, Multiline = true, PlaceholderText = "Cause of Expenditure" };

        var saveButton = new Button { Left = 20, Top = 460, Width = 160, Height = 34, Text = "Payment Made" };
        saveButton.Click += async (_, _) => await SaveAsync();

        Controls.Add(voucherNoLabel);
        Controls.Add(voucherDatePicker);
        Controls.Add(paidTowardsComboBox);
        Controls.Add(receiverTextBox);
        Controls.Add(paidByTextBox);
        Controls.Add(amountTextBox);
        Controls.Add(expenditureTextBox);
        Controls.Add(saveButton);

        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await RefreshVoucherNoAsync();
        var names = await paymentNameRepository.GetAllAsync();
        paidTowardsComboBox.DataSource = names.ToList();
        paidTowardsComboBox.DisplayMember = nameof(PaymentName.Name);
        paidTowardsComboBox.ValueMember = nameof(PaymentName.Name);
        voucherDatePicker.Value = DateTime.Today;
    }

    private async Task RefreshVoucherNoAsync()
    {
        currentVoucherNo = await paymentVoucherRepository.GetNextVoucherNoAsync();
        voucherNoLabel.Text = $"Voucher No: {currentVoucherNo}";
    }

    private async Task SaveAsync()
    {
        if (paidTowardsComboBox.SelectedItem is not PaymentName paymentName)
        {
            MessageBox.Show("Select Paid Towards.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(amountTextBox.Text, out var amount) || amount <= 0)
        {
            MessageBox.Show("Enter valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var voucher = new PaymentVoucher
        {
            VoucherNo = currentVoucherNo,
            VoucherDate = voucherDatePicker.Value.Date,
            PaidTowards = paymentName.Name,
            ReceiverName = receiverTextBox.Text.Trim(),
            PaidBy = paidByTextBox.Text.Trim(),
            AmountPaid = amount,
            ExpenditureCause = expenditureTextBox.Text.Trim()
        };

        await paymentVoucherRepository.AddAsync(voucher);
        MessageBox.Show("Payment voucher saved.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);

        receiverTextBox.Clear();
        paidByTextBox.Clear();
        amountTextBox.Clear();
        expenditureTextBox.Clear();
        voucherDatePicker.Value = DateTime.Today;
        await RefreshVoucherNoAsync();
    }
}