using DoctorSoft.Domain.Contracts;

namespace DoctorSoft.App.Forms;

public sealed class DrugReferenceForm : Form
{
    private readonly IMedicineRepository medicineRepository;
    private readonly TextBox searchTextBox;
    private readonly DataGridView grid;

    public DrugReferenceForm(IMedicineRepository medicineRepository)
    {
        this.medicineRepository = medicineRepository;

        Text = "Drug Reference";
        StartPosition = FormStartPosition.CenterParent;
        Width = 760;
        Height = 520;
        MinimizeBox = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        searchTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search medicine name..."
        };
        searchTextBox.TextChanged += async (_, _) => await LoadGridAsync(searchTextBox.Text);

        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Medicine",
            HeaderText = "Medicine",
            Width = 460
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Type",
            HeaderText = "Type",
            Width = 200
        });

        root.Controls.Add(searchTextBox, 0, 0);
        root.Controls.Add(grid, 0, 1);

        Controls.Add(root);

        Load += async (_, _) => await LoadGridAsync();
    }

    private async Task LoadGridAsync(string? filter = null)
    {
        var all = await medicineRepository.GetAllAsync();
        var rows = string.IsNullOrWhiteSpace(filter)
            ? all
            : all.Where(x => x.Medicine.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        grid.DataSource = rows;
    }
}
