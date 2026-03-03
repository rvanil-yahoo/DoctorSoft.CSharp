using System.Globalization;
using System.Text.RegularExpressions;

namespace DoctorSoft.App.Forms;

public sealed class MaintenanceHistoryForm : Form
{
    private readonly string logDirectory;
    private readonly int maxLogFilesToScan;
    private readonly TextBox userFilterTextBox;
    private readonly ComboBox actionFilterComboBox;
    private readonly ComboBox maxRowsComboBox;
    private readonly DateTimePicker fromDatePicker;
    private readonly DateTimePicker toDatePicker;
    private readonly Button todayButton;
    private readonly Button last7DaysButton;
    private readonly Button thisMonthButton;
    private readonly Button clearFiltersButton;
    private readonly Button loadButton;
    private readonly Button exportButton;
    private readonly DataGridView historyGrid;
    private readonly Label rowCountLabel;
    private List<MaintenanceHistoryRow> filteredRows = new();
    private List<MaintenanceHistoryRow> currentRows = new();
    private string sortColumn = nameof(MaintenanceHistoryRow.Timestamp);
    private bool sortAscending;

    public MaintenanceHistoryForm(string logDirectory, int maxLogFilesToScan, int defaultMaxRows)
    {
        this.logDirectory = logDirectory;
        this.maxLogFilesToScan = maxLogFilesToScan > 0 ? maxLogFilesToScan : 15;

        Text = "Maintenance Action History";
        Width = 1500;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 78 };

        userFilterTextBox = new TextBox
        {
            Left = 20,
            Top = 24,
            Width = 220,
            PlaceholderText = "User filter"
        };

        actionFilterComboBox = new ComboBox
        {
            Left = 255,
            Top = 24,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        actionFilterComboBox.Items.AddRange(new object[]
        {
            "All Actions",
            "Update",
            "Delete"
        });
        actionFilterComboBox.SelectedIndex = 0;

        var maxRowsLabel = new Label
        {
            Left = 450,
            Top = 28,
            Width = 58,
            Text = "Max Rows"
        };

        maxRowsComboBox = new ComboBox
        {
            Left = 510,
            Top = 24,
            Width = 80,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        maxRowsComboBox.Items.AddRange(new object[] { "100", "500", "2000" });
        var defaultMaxRowsText = defaultMaxRows.ToString();
        if (!maxRowsComboBox.Items.Contains(defaultMaxRowsText))
        {
            maxRowsComboBox.Items.Add(defaultMaxRowsText);
        }
        maxRowsComboBox.SelectedItem = defaultMaxRowsText;
        maxRowsComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (!IsHandleCreated)
            {
                return;
            }

            ApplySortAndBind();
        };

        var fromLabel = new Label
        {
            Left = 600,
            Top = 28,
            Width = 40,
            Text = "From"
        };

        fromDatePicker = new DateTimePicker
        {
            Left = 640,
            Top = 24,
            Width = 130,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(-7)
        };

        var toLabel = new Label
        {
            Left = 780,
            Top = 28,
            Width = 24,
            Text = "To"
        };

        toDatePicker = new DateTimePicker
        {
            Left = 805,
            Top = 24,
            Width = 130,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };

        todayButton = new Button
        {
            Left = 950,
            Top = 22,
            Width = 75,
            Height = 32,
            Text = "Today"
        };
        todayButton.Click += (_, _) =>
        {
            fromDatePicker.Value = DateTime.Today;
            toDatePicker.Value = DateTime.Today;
            LoadRows();
        };

        last7DaysButton = new Button
        {
            Left = 1030,
            Top = 22,
            Width = 95,
            Height = 32,
            Text = "Last 7 Days"
        };
        last7DaysButton.Click += (_, _) =>
        {
            fromDatePicker.Value = DateTime.Today.AddDays(-6);
            toDatePicker.Value = DateTime.Today;
            LoadRows();
        };

        thisMonthButton = new Button
        {
            Left = 1130,
            Top = 22,
            Width = 90,
            Height = 32,
            Text = "This Month"
        };
        thisMonthButton.Click += (_, _) =>
        {
            var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            fromDatePicker.Value = first;
            toDatePicker.Value = DateTime.Today;
            LoadRows();
        };

        clearFiltersButton = new Button
        {
            Left = 1225,
            Top = 22,
            Width = 90,
            Height = 32,
            Text = "Clear"
        };
        clearFiltersButton.Click += (_, _) =>
        {
            userFilterTextBox.Text = string.Empty;
            actionFilterComboBox.SelectedIndex = 0;
            fromDatePicker.Value = DateTime.Today.AddDays(-7);
            toDatePicker.Value = DateTime.Today;
            LoadRows();
        };

        loadButton = new Button
        {
            Left = 1320,
            Top = 22,
            Width = 60,
            Height = 32,
            Text = "Load"
        };
        loadButton.Click += (_, _) => LoadRows();

        exportButton = new Button
        {
            Left = 1385,
            Top = 22,
            Width = 95,
            Height = 32,
            Text = "Export CSV"
        };
        exportButton.Click += (_, _) => ExportCsv();

        rowCountLabel = new Label
        {
            Left = 20,
            Top = 58,
            Width = 520,
            Text = "Displayed 0 of 0 rows"
        };

        filterPanel.Controls.Add(userFilterTextBox);
        filterPanel.Controls.Add(actionFilterComboBox);
        filterPanel.Controls.Add(maxRowsLabel);
        filterPanel.Controls.Add(maxRowsComboBox);
        filterPanel.Controls.Add(fromLabel);
        filterPanel.Controls.Add(fromDatePicker);
        filterPanel.Controls.Add(toLabel);
        filterPanel.Controls.Add(toDatePicker);
        filterPanel.Controls.Add(todayButton);
        filterPanel.Controls.Add(last7DaysButton);
        filterPanel.Controls.Add(thisMonthButton);
        filterPanel.Controls.Add(clearFiltersButton);
        filterPanel.Controls.Add(loadButton);
        filterPanel.Controls.Add(exportButton);
        filterPanel.Controls.Add(rowCountLabel);

        historyGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        historyGrid.ColumnHeaderMouseClick += (_, args) => OnColumnHeaderClicked(args.ColumnIndex);
        historyGrid.CellDoubleClick += (_, args) => ShowRowDetails(args.RowIndex);

        Controls.Add(historyGrid);
        Controls.Add(filterPanel);

        Shown += (_, _) => LoadRows();
    }

    private void LoadRows()
    {
        loadButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var allRows = ReadHistoryRows();
            var userFilter = userFilterTextBox.Text.Trim();
            var actionFilter = actionFilterComboBox.SelectedIndex;
            var fromDate = fromDatePicker.Value.Date;
            var toDate = toDatePicker.Value.Date;

            if (fromDate > toDate)
            {
                MessageBox.Show("From date cannot be after To date.", "History", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            allRows = allRows
                .Where(row => row.Timestamp != DateTime.MinValue)
                .Where(row => row.Timestamp.Date >= fromDate && row.Timestamp.Date <= toDate)
                .ToList();

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                allRows = allRows
                    .Where(row => row.User.Contains(userFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (actionFilter == 1)
            {
                allRows = allRows.Where(row => row.Action.Equals("Update", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (actionFilter == 2)
            {
                allRows = allRows.Where(row => row.Action.Equals("Delete", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            filteredRows = allRows.ToList();
            ApplySortAndBind();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to load maintenance history: {exception.Message}", "History", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void OnColumnHeaderClicked(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= historyGrid.Columns.Count)
        {
            return;
        }

        var column = historyGrid.Columns[columnIndex];
        var clickedColumn = string.IsNullOrWhiteSpace(column.DataPropertyName) ? column.Name : column.DataPropertyName;
        if (string.IsNullOrWhiteSpace(clickedColumn))
        {
            return;
        }

        if (string.Equals(sortColumn, clickedColumn, StringComparison.Ordinal))
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortColumn = clickedColumn;
            sortAscending = true;
        }

        ApplySortAndBind();
    }

    private void ShowRowDetails(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= historyGrid.Rows.Count)
        {
            return;
        }

        if (historyGrid.Rows[rowIndex].DataBoundItem is not MaintenanceHistoryRow row)
        {
            return;
        }

        var timestampText = row.Timestamp == DateTime.MinValue
            ? "N/A"
            : row.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

        var details =
            "Timestamp: " + timestampText + Environment.NewLine +
            "User: " + row.User + Environment.NewLine +
            "Action: " + row.Action + Environment.NewLine + Environment.NewLine +
            "Message:" + Environment.NewLine + row.Message;

        MessageBox.Show(details, "History Entry Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ApplySortAndBind()
    {
        var maxRows = GetMaxRows();
        IEnumerable<MaintenanceHistoryRow> query = filteredRows;

        query = sortColumn switch
        {
            nameof(MaintenanceHistoryRow.User) => sortAscending
                ? query.OrderBy(row => row.User, StringComparer.OrdinalIgnoreCase)
                : query.OrderByDescending(row => row.User, StringComparer.OrdinalIgnoreCase),
            nameof(MaintenanceHistoryRow.Action) => sortAscending
                ? query.OrderBy(row => row.Action, StringComparer.OrdinalIgnoreCase)
                : query.OrderByDescending(row => row.Action, StringComparer.OrdinalIgnoreCase),
            nameof(MaintenanceHistoryRow.Message) => sortAscending
                ? query.OrderBy(row => row.Message, StringComparer.OrdinalIgnoreCase)
                : query.OrderByDescending(row => row.Message, StringComparer.OrdinalIgnoreCase),
            _ => sortAscending
                ? query.OrderBy(row => row.Timestamp)
                : query.OrderByDescending(row => row.Timestamp)
        };

        var sortedRows = query.Take(maxRows).ToList();
        currentRows = sortedRows;
        historyGrid.DataSource = sortedRows;
        rowCountLabel.Text = $"Displayed {currentRows.Count} of {filteredRows.Count} rows";

        foreach (DataGridViewColumn column in historyGrid.Columns)
        {
            column.HeaderCell.SortGlyphDirection = SortOrder.None;
        }

        var boundColumn = historyGrid.Columns
            .Cast<DataGridViewColumn>()
            .FirstOrDefault(c =>
                string.Equals(c.DataPropertyName, sortColumn, StringComparison.Ordinal) ||
                string.Equals(c.Name, sortColumn, StringComparison.Ordinal));

        if (boundColumn is not null)
        {
            boundColumn.HeaderCell.SortGlyphDirection = sortAscending
                ? SortOrder.Ascending
                : SortOrder.Descending;
        }
    }

    private int GetMaxRows()
    {
        if (maxRowsComboBox.SelectedItem is string text && int.TryParse(text, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return 500;
    }

    private void ExportCsv()
    {
        if (currentRows.Count == 0)
        {
            MessageBox.Show("No rows to export.", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"maintenance-history-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Title = "Export Maintenance History"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            using var writer = new StreamWriter(dialog.FileName, false);
            writer.WriteLine("Timestamp,User,Action,Message");

            foreach (var row in currentRows)
            {
                writer.WriteLine(
                    string.Join(",",
                        EscapeCsv(row.Timestamp == DateTime.MinValue ? string.Empty : row.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                        EscapeCsv(row.User),
                        EscapeCsv(row.Action),
                        EscapeCsv(row.Message)));
            }

            MessageBox.Show("History exported successfully.", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to export CSV: {exception.Message}", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        return value;
    }

    private List<MaintenanceHistoryRow> ReadHistoryRows()
    {
        if (!Directory.Exists(logDirectory))
        {
            return new List<MaintenanceHistoryRow>();
        }

        var files = Directory
            .GetFiles(logDirectory, "doctorsoft-*.log")
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(maxLogFilesToScan)
            .ToList();

        var results = new List<MaintenanceHistoryRow>();
        foreach (var file in files)
        {
            foreach (var line in File.ReadLines(file))
            {
                if (!line.Contains("Accounting maintenance ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var row = ParseLine(line);
                if (row is not null)
                {
                    results.Add(row);
                }
            }
        }

        return results;
    }

    private static MaintenanceHistoryRow? ParseLine(string line)
    {
        var message = line;
        DateTime timestamp = DateTime.MinValue;

        var match = Regex.Match(line, @"^(?<ts>\d{4}-\d{2}-\d{2}[^\[]*)\s*\[(?<level>[^\]]+)\]\s*(?<msg>.*)$");
        if (match.Success)
        {
            var tsRaw = match.Groups["ts"].Value.Trim();
            DateTime.TryParse(tsRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out timestamp);
            message = match.Groups["msg"].Value.Trim();
        }

        var action = message.Contains(" delete ", StringComparison.OrdinalIgnoreCase) ? "Delete" : "Update";
        var userMatch = Regex.Match(message, "by\\s+(?<user>[^:]+):", RegexOptions.IgnoreCase);
        var user = userMatch.Success ? userMatch.Groups["user"].Value.Trim() : "unknown";

        return new MaintenanceHistoryRow
        {
            Timestamp = timestamp,
            User = user,
            Action = action,
            Message = message
        };
    }

    private sealed class MaintenanceHistoryRow
    {
        public DateTime Timestamp { get; init; }
        public string User { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
