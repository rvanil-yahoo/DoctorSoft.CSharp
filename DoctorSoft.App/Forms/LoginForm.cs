using DoctorSoft.Domain.Contracts;
using Serilog;

namespace DoctorSoft.App.Forms;

public sealed class LoginForm : Form
{
    private readonly IAuthenticationService authenticationService;
    private readonly TextBox userNameTextBox;
    private readonly TextBox passwordTextBox;
    private readonly Button loginButton;
    private readonly Button cancelButton;

    public string AuthenticatedUserName { get; private set; } = string.Empty;

    public LoginForm(IAuthenticationService authenticationService)
    {
        this.authenticationService = authenticationService;

        Text = "DoctorSoft - Login";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 170);

        var userLabel = new Label { Text = "User Name:", Left = 20, Top = 25, Width = 90 };
        userNameTextBox = new TextBox { Left = 120, Top = 20, Width = 270 };

        var passwordLabel = new Label { Text = "Password:", Left = 20, Top = 65, Width = 90 };
        passwordTextBox = new TextBox { Left = 120, Top = 60, Width = 270, UseSystemPasswordChar = true };

        loginButton = new Button { Text = "Login", Left = 120, Top = 110, Width = 120 };
        cancelButton = new Button { Text = "Cancel", Left = 270, Top = 110, Width = 120 };

        loginButton.Click += async (_, _) => await HandleLoginAsync();
        cancelButton.Click += (_, _) => Close();

        Controls.Add(userLabel);
        Controls.Add(userNameTextBox);
        Controls.Add(passwordLabel);
        Controls.Add(passwordTextBox);
        Controls.Add(loginButton);
        Controls.Add(cancelButton);

        AcceptButton = loginButton;
        CancelButton = cancelButton;
    }

    private async Task HandleLoginAsync()
    {
        loginButton.Enabled = false;

        try
        {
            var result = await authenticationService.SignInAsync(userNameTextBox.Text, passwordTextBox.Text);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                passwordTextBox.Focus();
                passwordTextBox.SelectAll();
                return;
            }

            AuthenticatedUserName = userNameTextBox.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Login failed due to runtime error.");
            MessageBox.Show(
                "Login could not be completed due to a runtime configuration issue. Check logs for details.",
                "Login Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            loginButton.Enabled = true;
        }
    }
}
