using DoctorSoft.Domain.Contracts;
using DoctorSoft.Domain.Models;
using Serilog;

namespace DoctorSoft.App.Forms;

public sealed class AccountingMaintenanceForm : Form
{
    private readonly IAccountingMaintenanceRepository accountingMaintenanceRepository;
    private readonly string actorUserName;
    private readonly string logDirectory;
    private readonly int maintenanceHistoryFileScanLimit;
    private readonly int maintenanceHistoryDefaultMaxRows;
    private readonly ComboBox modeComboBox;
    private readonly TextBox ledgerNameTextBox;
    private readonly DateTimePicker fromDatePicker;
    private readonly DateTimePicker toDatePicker;
    private readonly Button loadButton;
    private readonly Button historyButton;
    private readonly Button editButton;
    private readonly Button deleteButton;
    private readonly DataGridView recordsGrid;

    private IReadOnlyList<PaymentMaintenanceRecord> paymentRows = Array.Empty<PaymentMaintenanceRecord>();
    private IReadOnlyList<ReceiptMaintenanceRecord> receiptRows = Array.Empty<ReceiptMaintenanceRecord>();
    private IReadOnlyList<LedgerMaintenanceRecord> ledgerRows = Array.Empty<LedgerMaintenanceRecord>();

    public AccountingMaintenanceForm(
        IAccountingMaintenanceRepository accountingMaintenanceRepository,
        string actorUserName,
        string logDirectory,
        int maintenanceHistoryFileScanLimit,
        int maintenanceHistoryDefaultMaxRows)
    {
        this.accountingMaintenanceRepository = accountingMaintenanceRepository;
        this.actorUserName = string.IsNullOrWhiteSpace(actorUserName) ? "unknown" : actorUserName.Trim();
        this.logDirectory = logDirectory;
        this.maintenanceHistoryFileScanLimit = maintenanceHistoryFileScanLimit;
        this.maintenanceHistoryDefaultMaxRows = maintenanceHistoryDefaultMaxRows;

        Text = "Accounting Maintenance";
        Width = 1250;
        Height = 730;
        StartPosition = FormStartPosition.CenterParent;

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88
        };

        modeComboBox = new ComboBox
        {
            Left = 20,
            Top = 24,
            Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        modeComboBox.Items.AddRange(new object[]
        {
            "Payment Vouchers",
            "Receipt Vouchers",
            "Ledger Entries"
        });
        modeComboBox.SelectedIndex = 0;

        ledgerNameTextBox = new TextBox
        {
            Left = 255,
            Top = 24,
            Width = 220,
            PlaceholderText = "Ledger name contains"
        };

        fromDatePicker = new DateTimePicker
        {
            Left = 490,
            Top = 24,
            Width = 140,
            Format = DateTimePickerFormat.Short
        };

        toDatePicker = new DateTimePicker
        {
            Left = 640,
            Top = 24,
            Width = 140,
            Format = DateTimePickerFormat.Short
        };

        loadButton = new Button
        {
            Left = 800,
            Top = 22,
            Width = 90,
            Height = 32,
            Text = "Load"
        };
        loadButton.Click += async (_, _) => await LoadAsync();

        historyButton = new Button
        {
            Left = 895,
            Top = 22,
            Width = 90,
            Height = 32,
            Text = "History"
        };
        historyButton.Click += (_, _) =>
        {
            using var form = new MaintenanceHistoryForm(logDirectory, maintenanceHistoryFileScanLimit, maintenanceHistoryDefaultMaxRows);
            form.ShowDialog(this);
        };

        editButton = new Button
        {
            Left = 990,
            Top = 22,
            Width = 105,
            Height = 32,
            Text = "Edit Selected"
        };
        editButton.Click += async (_, _) => await EditSelectedAsync();

        deleteButton = new Button
        {
            Left = 1100,
            Top = 22,
            Width = 120,
            Height = 32,
            Text = "Delete Selected"
        };
        deleteButton.Click += async (_, _) => await DeleteSelectedAsync();

        topPanel.Controls.Add(modeComboBox);
        topPanel.Controls.Add(ledgerNameTextBox);
        topPanel.Controls.Add(fromDatePicker);
        topPanel.Controls.Add(toDatePicker);
        topPanel.Controls.Add(loadButton);
        topPanel.Controls.Add(historyButton);
        topPanel.Controls.Add(editButton);
        topPanel.Controls.Add(deleteButton);

        recordsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        Controls.Add(recordsGrid);
        Controls.Add(topPanel);
    }

    private async Task LoadAsync()
    {
        loadButton.Enabled = false;
        editButton.Enabled = false;
        deleteButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var fromDate = fromDatePicker.Value.Date;
            var toDate = toDatePicker.Value.Date;
            var ledgerNameFilter = string.IsNullOrWhiteSpace(ledgerNameTextBox.Text)
                ? null
                : ledgerNameTextBox.Text.Trim();

            switch (modeComboBox.SelectedIndex)
            {
                case 0:
                    paymentRows = await accountingMaintenanceRepository.GetPaymentsAsync(fromDate, toDate, ledgerNameFilter);
                    recordsGrid.DataSource = paymentRows.ToList();
                    break;
                case 1:
                    receiptRows = await accountingMaintenanceRepository.GetReceiptsAsync(fromDate, toDate, ledgerNameFilter);
                    recordsGrid.DataSource = receiptRows.ToList();
                    break;
                default:
                    ledgerRows = await accountingMaintenanceRepository.GetLedgerEntriesAsync(fromDate, toDate, ledgerNameFilter);
                    recordsGrid.DataSource = ledgerRows.ToList();
                    break;
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Failed to load records: {exception.Message}", "Accounting Maintenance", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            editButton.Enabled = true;
            deleteButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private async Task EditSelectedAsync()
    {
        if (recordsGrid.CurrentRow is null)
        {
            MessageBox.Show("Select a row to edit.", "Accounting Maintenance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rowIndex = recordsGrid.CurrentRow.Index;

        switch (modeComboBox.SelectedIndex)
        {
            case 0:
                if (rowIndex < 0 || rowIndex >= paymentRows.Count)
                {
                    return;
                }

                using (var dialog = new VoucherEditForm(paymentRows[rowIndex]))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK || dialog.PaymentRecord is null)
                    {
                        return;
                    }

                    await accountingMaintenanceRepository.UpdatePaymentVoucherAsync(dialog.PaymentRecord);
                    Log.Information(
                        "Accounting maintenance update by {User}: Payment voucher {VoucherNo} updated (Date={Date}, Ledger={Ledger}, Amount={Amount}).",
                        actorUserName,
                        dialog.PaymentRecord.VoucherNo,
                        dialog.PaymentRecord.VoucherDate,
                        dialog.PaymentRecord.PaidTowards,
                        dialog.PaymentRecord.AmountPaid);
                }
                break;

            case 1:
                if (rowIndex < 0 || rowIndex >= receiptRows.Count)
                {
                    return;
                }

                using (var dialog = new VoucherEditForm(receiptRows[rowIndex]))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK || dialog.ReceiptRecord is null)
                    {
                        return;
                    }

                    await accountingMaintenanceRepository.UpdateReceiptVoucherAsync(dialog.ReceiptRecord);
                    Log.Information(
                        "Accounting maintenance update by {User}: Receipt voucher {VoucherNo} updated (Date={Date}, Ledger={Ledger}, Amount={Amount}).",
                        actorUserName,
                        dialog.ReceiptRecord.VoucherNo,
                        dialog.ReceiptRecord.VoucherDate,
                        dialog.ReceiptRecord.LedgerName,
                        dialog.ReceiptRecord.AmountReceived);
                }
                break;

            default:
                if (rowIndex < 0 || rowIndex >= ledgerRows.Count)
                {
                    return;
                }

                using (var dialog = new LedgerEntryEditForm(ledgerRows[rowIndex]))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK || dialog.UpdatedRecord is null)
                    {
                        return;
                    }

                    await accountingMaintenanceRepository.UpdateLedgerEntryAsync(dialog.UpdatedRecord);
                    Log.Information(
                        "Accounting maintenance update by {User}: Ledger entry {AutoId} updated (Date={Date}, Ledger={Ledger}, Debit={Debit}, Credit={Credit}).",
                        actorUserName,
                        dialog.UpdatedRecord.AutoId,
                        dialog.UpdatedRecord.Date,
                        dialog.UpdatedRecord.LedgerName,
                        dialog.UpdatedRecord.Debit,
                        dialog.UpdatedRecord.Credit);
                }
                break;
        }

        await LoadAsync();
    }

    private async Task DeleteSelectedAsync()
    {
        if (recordsGrid.CurrentRow is null)
        {
            MessageBox.Show("Select a row to delete.", "Accounting Maintenance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rowIndex = recordsGrid.CurrentRow.Index;
        var confirmation = MessageBox.Show(
            "Delete selected record and linked accounting rows? This action cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        deleteButton.Enabled = false;
        editButton.Enabled = false;
        loadButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            switch (modeComboBox.SelectedIndex)
            {
                case 0:
                    if (rowIndex < 0 || rowIndex >= paymentRows.Count)
                    {
                        return;
                    }

                    var paymentVoucherNo = paymentRows[rowIndex].VoucherNo;
                    await accountingMaintenanceRepository.DeletePaymentVoucherAsync(paymentVoucherNo);
                    Log.Information(
                        "Accounting maintenance delete by {User}: Payment voucher {VoucherNo} and linked debit ledger rows deleted.",
                        actorUserName,
                        paymentVoucherNo);
                    break;
                case 1:
                    if (rowIndex < 0 || rowIndex >= receiptRows.Count)
                    {
                        return;
                    }

                    var receiptVoucherNo = receiptRows[rowIndex].VoucherNo;
                    await accountingMaintenanceRepository.DeleteReceiptVoucherAsync(receiptVoucherNo);
                    Log.Information(
                        "Accounting maintenance delete by {User}: Receipt voucher {VoucherNo} and linked credit ledger rows deleted.",
                        actorUserName,
                        receiptVoucherNo);
                    break;
                default:
                    if (rowIndex < 0 || rowIndex >= ledgerRows.Count)
                    {
                        return;
                    }

                    var autoId = ledgerRows[rowIndex].AutoId;
                    await accountingMaintenanceRepository.DeleteLedgerEntryAsync(autoId);
                    Log.Information(
                        "Accounting maintenance delete by {User}: Ledger entry {AutoId} deleted.",
                        actorUserName,
                        autoId);
                    break;
            }

            await LoadAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Delete failed: {exception.Message}", "Accounting Maintenance", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            deleteButton.Enabled = true;
            editButton.Enabled = true;
            loadButton.Enabled = true;
            UseWaitCursor = false;
        }
    }
}
