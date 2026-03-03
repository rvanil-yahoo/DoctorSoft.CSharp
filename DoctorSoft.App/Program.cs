using DoctorSoft.App.Configuration;
using DoctorSoft.App.Forms;
using DoctorSoft.App.Security;
using DoctorSoft.Data.Access;
using DoctorSoft.Data.Repositories;
using DoctorSoft.Data.Security;
using DoctorSoft.Domain.Services;
using DoctorSoft.Domain.Contracts;
using DoctorSoft.Reports.Services;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DoctorSoft.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        var configuration = BuildConfiguration();
        var appSettings = configuration.GetSection("App").Get<AppSettings>() ?? new AppSettings();
        var smtpDefaults = configuration.GetSection("SmtpDefaults").Get<SmtpDefaults>() ?? new SmtpDefaults();
        var authOptions = configuration.GetSection("Authentication").Get<AuthenticationOptions>() ?? new AuthenticationOptions();
        var logDirectory = ResolveDirectoryPath(appSettings.LogDirectory, "logs");
        var backupDirectory = ResolveDirectoryPath(appSettings.BackupDirectory, "backups");

        if (authOptions.EnableDatabasePrimary && Environment.Version.Major < 8)
        {
            authOptions.EnableDatabasePrimary = false;
            MessageBox.Show(
                "Primary database authentication is disabled because this build is running on .NET 6. " +
                "Access ODBC/OLEDB providers require .NET 8+ in this migration setup. " +
                "Using fallback users from appsettings.json.",
                "DoctorSoft Configuration",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        Directory.CreateDirectory(logDirectory);
        Directory.CreateDirectory(backupDirectory);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logDirectory, "doctorsoft-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        WireGlobalExceptionHandlers();

        ApplicationConfiguration.Initialize();

        try
        {
            var dbPath = ResolveDatabasePath(configuration["Database:MainDbPath"]);
            var connectionFactory = new AccessConnectionFactory(dbPath);
            var primaryCredentialStore = new AccessUserCredentialStore(connectionFactory);
            var credentialStore = new ResilientUserCredentialStore(primaryCredentialStore, authOptions);
            var decoder = new LegacyPasswordDecoder();
            var authService = new AuthenticationService(credentialStore, decoder);
            IPatientRepository patientRepository = new AccessPatientRepository(connectionFactory);
            IAppointmentRepository appointmentRepository = new AccessAppointmentRepository(connectionFactory);
            IAppointmentReportService appointmentReportService = new AppointmentReportService(connectionFactory);
            IAppointmentRdlcExportService appointmentRdlcExportService = new AppointmentRdlcExportService();
            IPaymentRdlcExportService paymentRdlcExportService = new PaymentRdlcExportService();
            IReceiptRdlcExportService receiptRdlcExportService = new ReceiptRdlcExportService();
            IConsolidatedLedgerRdlcExportService consolidatedLedgerRdlcExportService = new ConsolidatedLedgerRdlcExportService();
            IMedicineRepository medicineRepository = new AccessMedicineRepository(connectionFactory);
            IPrescriptionRepository prescriptionRepository = new AccessPrescriptionRepository(connectionFactory);
            IPrescriptionReportService prescriptionReportService = new PrescriptionReportService(connectionFactory);
            IReferralRepository referralRepository = new AccessReferralRepository(connectionFactory);
            IObservationRepository observationRepository = new AccessObservationRepository(connectionFactory);
            IObservationReportService observationReportService = new ObservationReportService(connectionFactory);
            IPatientHistoryRepository patientHistoryRepository = new AccessPatientHistoryRepository(connectionFactory);
            IPatientHistoryReportService patientHistoryReportService = new PatientHistoryReportService(connectionFactory);
            IPaymentNameRepository paymentNameRepository = new AccessPaymentNameRepository(connectionFactory);
            IPaymentVoucherRepository paymentVoucherRepository = new AccessPaymentVoucherRepository(connectionFactory);
            IPaymentReportService paymentReportService = new PaymentReportService(connectionFactory);
            IReceiptNameRepository receiptNameRepository = new AccessReceiptNameRepository(connectionFactory);
            IReceiptVoucherRepository receiptVoucherRepository = new AccessReceiptVoucherRepository(connectionFactory);
            IReceiptReportService receiptReportService = new ReceiptReportService(connectionFactory);
            IConsolidatedLedgerReportService consolidatedLedgerReportService = new ConsolidatedLedgerReportService(connectionFactory);
            IAccountingMaintenanceRepository accountingMaintenanceRepository = new AccessAccountingMaintenanceRepository(connectionFactory);
            IUserAdministrationRepository userAdministrationRepository = new AccessUserAdministrationRepository(connectionFactory);

            using var loginForm = new LoginForm(authService);
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainShellForm(
                    loginForm.AuthenticatedUserName,
                    logDirectory,
                    backupDirectory,
                    appSettings.MaintenanceHistoryFileScanLimit,
                    appSettings.MaintenanceHistoryDefaultMaxRows,
                    dbPath,
                    authService,
                    patientRepository,
                    appointmentRepository,
                    appointmentReportService,
                    appointmentRdlcExportService,
                    paymentRdlcExportService,
                    receiptRdlcExportService,
                    consolidatedLedgerRdlcExportService,
                    medicineRepository,
                    prescriptionRepository,
                    prescriptionReportService,
                    referralRepository,
                    observationRepository,
                        observationReportService,
                        patientHistoryRepository,
                            patientHistoryReportService,
                            paymentNameRepository,
                            paymentVoucherRepository,
                                paymentReportService,
                                receiptNameRepository,
                                receiptVoucherRepository,
                                    receiptReportService,
                                    consolidatedLedgerReportService,
                                    accountingMaintenanceRepository,
                                    userAdministrationRepository,
                                    smtpDefaults));
            }
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Application startup failed.");
            MessageBox.Show(
                $"Application failed to start.{Environment.NewLine}{exception.Message}",
                "DoctorSoft",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static string ResolveDirectoryPath(string? configuredPath, string fallback)
    {
        var raw = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath.Trim();
        if (Path.IsPathRooted(raw))
        {
            return raw;
        }

        return Path.Combine(AppContext.BaseDirectory, raw);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
    }

    private static void WireGlobalExceptionHandlers()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI exception.");
            MessageBox.Show("An unexpected error occurred. Please check logs.", "DoctorSoft", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                Log.Error(exception, "Unhandled non-UI exception.");
            }
            else
            {
                Log.Error("Unhandled non-UI exception (non-Exception object).");
            }
        };
    }

    private static string ResolveDatabasePath(string? configuredPath)
    {
        var fallback = "MainDb.mdb";
        var rawPath = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath;

        if (Path.IsPathRooted(rawPath) && File.Exists(rawPath))
        {
            return rawPath;
        }

        var fileName = Path.GetFileName(rawPath);
        var start = new DirectoryInfo(AppContext.BaseDirectory);

        for (var level = 0; level < 8 && start is not null; level++)
        {
            var candidate = Path.Combine(start.FullName, rawPath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var fallbackCandidate = Path.Combine(start.FullName, fileName);
            if (File.Exists(fallbackCandidate))
            {
                return fallbackCandidate;
            }

            start = start.Parent;
        }

        var fallbackPath = Path.Combine(AppContext.BaseDirectory, rawPath);
        Log.Warning("Database file '{DatabasePath}' was not found during startup discovery. Continuing with configured path.", rawPath);
        return fallbackPath;
    }
}