using DoctorSoft.Domain.Contracts;
using Serilog;

namespace DoctorSoft.App.Forms;

public sealed class UserAdministrationForm : Form
{
    private readonly IUserAdministrationRepository userAdministrationRepository;
    private readonly string currentUserName;
    private readonly DataGridView usersGrid;
    private readonly TextBox userNameTextBox;
    private readonly TextBox passwordTextBox;
    private readonly Button loadButton;
    private readonly Button addButton;
    private readonly Button updatePasswordButton;
    private readonly Button deleteButton;

    private IReadOnlyList<Domain.Models.UserAdminRecord> users = Array.Empty<Domain.Models.UserAdminRecord>();

    public UserAdministrationForm(IUserAdministrationRepository userAdministrationRepository, string currentUserName)
    {
        this.userAdministrationRepository = userAdministrationRepository;
        this.currentUserName = currentUserName;

        Text = "User Administration";
        Width = 900;
        Height = 640;
        StartPosition = FormStartPosition.CenterParent;

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 84 };

        userNameTextBox = new TextBox
        {
            Left = 20,
            Top = 24,
            Width = 220,
            PlaceholderText = "User name"
        };

        passwordTextBox = new TextBox
        {
            Left = 255,
            Top = 24,
            Width = 220,
            PlaceholderText = "Password",
            UseSystemPasswordChar = true
        };

        loadButton = new Button { Left = 490, Top = 22, Width = 90, Height = 32, Text = "Load" };
        addButton = new Button { Left = 590, Top = 22, Width = 90, Height = 32, Text = "Add" };
        updatePasswordButton = new Button { Left = 690, Top = 22, Width = 120, Height = 32, Text = "Set Password" };
        deleteButton = new Button { Left = 820, Top = 22, Width = 60, Height = 32, Text = "Delete" };

        loadButton.Click += async (_, _) => await LoadUsersAsync();
        addButton.Click += async (_, _) => await AddUserAsync();
        updatePasswordButton.Click += async (_, _) => await UpdatePasswordAsync();
        deleteButton.Click += async (_, _) => await DeleteUserAsync();

        topPanel.Controls.Add(userNameTextBox);
        topPanel.Controls.Add(passwordTextBox);
        topPanel.Controls.Add(loadButton);
        topPanel.Controls.Add(addButton);
        topPanel.Controls.Add(updatePasswordButton);
        topPanel.Controls.Add(deleteButton);

        usersGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        usersGrid.SelectionChanged += (_, _) => FillSelectedUser();

        Controls.Add(usersGrid);
        Controls.Add(topPanel);

        Shown += async (_, _) => await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        ToggleBusy(true);
        try
        {
            users = await userAdministrationRepository.GetUsersAsync();
            usersGrid.DataSource = users.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to load users: {exception.Message}", "Users", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void FillSelectedUser()
    {
        if (usersGrid.CurrentRow?.DataBoundItem is not Domain.Models.UserAdminRecord selected)
        {
            return;
        }

        userNameTextBox.Text = selected.UserName;
    }

    private async Task AddUserAsync()
    {
        var userName = userNameTextBox.Text.Trim();
        var password = passwordTextBox.Text;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("User name and password are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ToggleBusy(true);
        try
        {
            await userAdministrationRepository.AddUserAsync(userName, password);
            Log.Information("User administration by {Actor}: added user {TargetUser}.", currentUserName, userName);
            MessageBox.Show("User added.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
            passwordTextBox.Text = string.Empty;
            await LoadUsersAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to add user: {exception.Message}", "Users", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private async Task UpdatePasswordAsync()
    {
        var userName = userNameTextBox.Text.Trim();
        var password = passwordTextBox.Text;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("User name and password are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ToggleBusy(true);
        try
        {
            await userAdministrationRepository.UpdatePasswordAsync(userName, password);
            Log.Information("User administration by {Actor}: updated password for user {TargetUser}.", currentUserName, userName);
            MessageBox.Show("Password updated.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
            passwordTextBox.Text = string.Empty;
            await LoadUsersAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to update password: {exception.Message}", "Users", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private async Task DeleteUserAsync()
    {
        var userName = userNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            MessageBox.Show("Select a user to delete.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.Equals(userName, currentUserName, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Current logged-in user cannot be deleted.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirmation = MessageBox.Show(
            $"Delete user '{userName}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        ToggleBusy(true);
        try
        {
            await userAdministrationRepository.DeleteUserAsync(userName);
            Log.Information("User administration by {Actor}: deleted user {TargetUser}.", currentUserName, userName);
            MessageBox.Show("User deleted.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
            userNameTextBox.Text = string.Empty;
            passwordTextBox.Text = string.Empty;
            await LoadUsersAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to delete user: {exception.Message}", "Users", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void ToggleBusy(bool busy)
    {
        loadButton.Enabled = !busy;
        addButton.Enabled = !busy;
        updatePasswordButton.Enabled = !busy;
        deleteButton.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
