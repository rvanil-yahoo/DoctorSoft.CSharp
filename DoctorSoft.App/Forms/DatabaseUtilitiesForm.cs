using Serilog;

namespace DoctorSoft.App.Forms;

public sealed class DatabaseUtilitiesForm : Form
{
    private readonly string databasePath;
    private readonly string backupDirectory;
    private readonly TextBox databasePathTextBox;
    private readonly TextBox backupPathTextBox;
    private readonly Button backupButton;
    private readonly Button restoreButton;

    public DatabaseUtilitiesForm(string databasePath, string backupDirectory)
    {
        this.databasePath = databasePath;
        this.backupDirectory = backupDirectory;

        Text = "Database Utilities";
        Width = 760;
        Height = 260;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label { Left = 20, Top = 28, Width = 130, Text = "Main DB Path" });
        databasePathTextBox = new TextBox
        {
            Left = 155,
            Top = 24,
            Width = 570,
            ReadOnly = true,
            Text = databasePath
        };

        Controls.Add(new Label { Left = 20, Top = 60, Width = 130, Text = "Backup Folder" });
        backupPathTextBox = new TextBox
        {
            Left = 155,
            Top = 56,
            Width = 570,
            ReadOnly = true,
            Text = backupDirectory
        };

        backupButton = new Button
        {
            Left = 155,
            Top = 106,
            Width = 150,
            Height = 36,
            Text = "Backup Database"
        };
        backupButton.Click += (_, _) => BackupDatabase();

        restoreButton = new Button
        {
            Left = 320,
            Top = 106,
            Width = 150,
            Height = 36,
            Text = "Restore Database"
        };
        restoreButton.Click += (_, _) => RestoreDatabase();

        Controls.Add(databasePathTextBox);
        Controls.Add(backupPathTextBox);
        Controls.Add(backupButton);
        Controls.Add(restoreButton);
    }

    private void BackupDatabase()
    {
        if (!File.Exists(databasePath))
        {
            MessageBox.Show("Database file not found.", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Access Database (*.mdb)|*.mdb|All files (*.*)|*.*",
            FileName = $"MainDb_backup_{DateTime.Now:yyyyMMdd_HHmmss}.mdb",
            InitialDirectory = Directory.Exists(backupDirectory) ? backupDirectory : null,
            Title = "Save Database Backup"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            File.Copy(databasePath, dialog.FileName, overwrite: true);
            Log.Information("Database backup created from {SourcePath} to {DestinationPath}.", databasePath, dialog.FileName);
            MessageBox.Show("Database backup completed.", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Backup failed: {exception.Message}", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestoreDatabase()
    {
        if (!File.Exists(databasePath))
        {
            MessageBox.Show("Target database file not found.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var confirmation = MessageBox.Show(
            "Restore will overwrite the current database file. Continue?",
            "Confirm Restore",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "Access Database (*.mdb)|*.mdb|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(backupDirectory) ? backupDirectory : null,
            Title = "Select Backup to Restore"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            File.Copy(dialog.FileName, databasePath, overwrite: true);
            Log.Information("Database restored from {SourcePath} to {DestinationPath}.", dialog.FileName, databasePath);
            MessageBox.Show(
                "Database restored successfully. Restart the application to ensure fresh connections.",
                "Restore",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Restore failed: {exception.Message}", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
