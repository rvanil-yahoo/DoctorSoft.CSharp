using System.Net;
using System.Net.Mail;
using DoctorSoft.App.Configuration;

namespace DoctorSoft.App.Forms;

public sealed class EmailCenterForm : Form
{
    public EmailCenterForm(SmtpDefaults defaults)
    {
        Text = "Email Center";
        StartPosition = FormStartPosition.CenterParent;
        Width = 760;
        Height = 620;
        MinimizeBox = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(10)
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 2; i++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        }
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var toEmail = new TextBox { Dock = DockStyle.Fill, Text = defaults.FromEmail ?? string.Empty };
        var subject = new TextBox { Dock = DockStyle.Fill };
        var body = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };

        root.Controls.Add(new Label { Text = "To", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        root.Controls.Add(toEmail, 1, 0);
        root.Controls.Add(new Label { Text = "Subject", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        root.Controls.Add(subject, 1, 1);
        root.Controls.Add(new Label { Text = "Message", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        root.Controls.Add(body, 1, 2);

        var sendButton = new Button
        {
            Text = "Send Email",
            Width = 120,
            Height = 30,
            Anchor = AnchorStyles.Right
        };

        sendButton.Click += async (_, _) =>
        {
            sendButton.Enabled = false;
            try
            {
                var smtpHost = string.IsNullOrWhiteSpace(defaults.Host) ? string.Empty : defaults.Host.Trim();
                var smtpPort = defaults.Port > 0 ? defaults.Port : 587;
                var smtpUser = string.IsNullOrWhiteSpace(defaults.UserName) ? string.Empty : defaults.UserName.Trim();
                var smtpPassword = defaults.Password ?? string.Empty;
                var fromAddress = string.IsNullOrWhiteSpace(defaults.FromEmail) ? smtpUser : defaults.FromEmail.Trim();

                if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPassword))
                {
                    MessageBox.Show(this, "SMTP defaults are incomplete. Please set SmtpDefaults.Host, UserName, and Password in appsettings.json.", "Email Center", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(fromAddress) || string.IsNullOrWhiteSpace(toEmail.Text))
                {
                    MessageBox.Show(this, "From/To email is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var message = new MailMessage(fromAddress, toEmail.Text.Trim(), subject.Text.Trim(), body.Text);
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = defaults.EnableSsl,
                    Credentials = new NetworkCredential(smtpUser, smtpPassword)
                };

                await client.SendMailAsync(message);
                MessageBox.Show(this, "Email sent successfully.", "Email Center", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to send email:\n" + ex.Message, "Email Center", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                sendButton.Enabled = true;
            }
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttonPanel.Controls.Add(sendButton);
        root.Controls.Add(buttonPanel, 1, 3);

        Controls.Add(root);
    }
}
