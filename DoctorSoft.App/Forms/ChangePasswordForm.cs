using DoctorSoft.Domain.Contracts;
using Serilog;

namespace DoctorSoft.App.Forms;

public sealed class ChangePasswordForm : Form
{
    private readonly string currentUserName;
    private readonly IAuthenticationService authenticationService;
    private readonly IUserAdministrationRepository userAdministrationRepository;

    private readonly TextBox currentPasswordTextBox;
    private readonly TextBox newPasswordTextBox;
    private readonly TextBox confirmPasswordTextBox;
    private readonly Button saveButton;

    public ChangePasswordForm(
        string currentUserName,
        IAuthenticationService authenticationService,
        IUserAdministrationRepository userAdministrationRepository)
    {
        this.currentUserName = currentUserName;
        this.authenticationService = authenticationService;
        this.userAdministrationRepository = userAdministrationRepository;

        Text = "Change Password";
        Width = 520;
        Height = 300;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(new Label { Left = 24, Top = 32, Width = 160, Text = "Current Password" });
        currentPasswordTextBox = new TextBox
        {
            Left = 190,
            Top = 28,
            Width = 280,
            UseSystemPasswordChar = true
        };

        Controls.Add(new Label { Left = 24, Top = 72, Width = 160, Text = "New Password" });
        newPasswordTextBox = new TextBox
        {
            Left = 190,
            Top = 68,
            Width = 280,
            UseSystemPasswordChar = true
        };

        Controls.Add(new Label { Left = 24, Top = 112, Width = 160, Text = "Confirm New Password" });
        confirmPasswordTextBox = new TextBox
        {
            Left = 190,
            Top = 108,
            Width = 280,
            UseSystemPasswordChar = true
        };

        saveButton = new Button
        {
            Left = 375,
            Top = 168,
            Width = 95,
            Height = 32,
            Text = "Save"
        };
        saveButton.Click += async (_, _) => await SaveAsync();

        Controls.Add(currentPasswordTextBox);
        Controls.Add(newPasswordTextBox);
        Controls.Add(confirmPasswordTextBox);
        Controls.Add(saveButton);
    }

    private async Task SaveAsync()
    {
        var currentPassword = currentPasswordTextBox.Text;
        var newPassword = newPasswordTextBox.Text;
        var confirmPassword = confirmPasswordTextBox.Text;

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            MessageBox.Show("Current and new password are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            MessageBox.Show("New password and confirmation do not match.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        saveButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var authResult = await authenticationService.SignInAsync(currentUserName, currentPassword);
            if (!authResult.Success)
            {
                MessageBox.Show("Current password is incorrect.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await userAdministrationRepository.UpdatePasswordAsync(currentUserName, newPassword);
            Log.Information("Password changed by user {User}.", currentUserName);
            MessageBox.Show("Password changed successfully.", "Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to change password: {exception.Message}", "Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            saveButton.Enabled = true;
            UseWaitCursor = false;
        }
    }
}
