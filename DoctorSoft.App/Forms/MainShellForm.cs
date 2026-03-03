using DoctorSoft.Domain.Contracts;
using DoctorSoft.App.Configuration;

namespace DoctorSoft.App.Forms;

public sealed class MainShellForm : Form
{
    private readonly string logDirectory;
    private readonly string backupDirectory;
    private readonly int maintenanceHistoryFileScanLimit;
    private readonly int maintenanceHistoryDefaultMaxRows;
    private readonly string databasePath;
    private readonly IAuthenticationService authenticationService;
    private readonly IPatientRepository patientRepository;
    private readonly IAppointmentRepository appointmentRepository;
    private readonly IAppointmentReportService appointmentReportService;
    private readonly IAppointmentRdlcExportService appointmentRdlcExportService;
    private readonly IPaymentRdlcExportService paymentRdlcExportService;
    private readonly IReceiptRdlcExportService receiptRdlcExportService;
    private readonly IConsolidatedLedgerRdlcExportService consolidatedLedgerRdlcExportService;
    private readonly IMedicineRepository medicineRepository;
    private readonly IPrescriptionRepository prescriptionRepository;
    private readonly IPrescriptionReportService prescriptionReportService;
    private readonly IReferralRepository referralRepository;
    private readonly IObservationRepository observationRepository;
    private readonly IObservationReportService observationReportService;
    private readonly IPatientHistoryRepository patientHistoryRepository;
    private readonly IPatientHistoryReportService patientHistoryReportService;
    private readonly IPaymentNameRepository paymentNameRepository;
    private readonly IPaymentVoucherRepository paymentVoucherRepository;
    private readonly IPaymentReportService paymentReportService;
    private readonly IReceiptNameRepository receiptNameRepository;
    private readonly IReceiptVoucherRepository receiptVoucherRepository;
    private readonly IReceiptReportService receiptReportService;
    private readonly IConsolidatedLedgerReportService consolidatedLedgerReportService;
    private readonly IAccountingMaintenanceRepository accountingMaintenanceRepository;
    private readonly IUserAdministrationRepository userAdministrationRepository;
    private readonly SmtpDefaults smtpDefaults;

    public MainShellForm(
        string userName,
        string logDirectory,
        string backupDirectory,
        int maintenanceHistoryFileScanLimit,
        int maintenanceHistoryDefaultMaxRows,
        string databasePath,
        IAuthenticationService authenticationService,
        IPatientRepository patientRepository,
        IAppointmentRepository appointmentRepository,
        IAppointmentReportService appointmentReportService,
        IAppointmentRdlcExportService appointmentRdlcExportService,
        IPaymentRdlcExportService paymentRdlcExportService,
        IReceiptRdlcExportService receiptRdlcExportService,
        IConsolidatedLedgerRdlcExportService consolidatedLedgerRdlcExportService,
        IMedicineRepository medicineRepository,
        IPrescriptionRepository prescriptionRepository,
        IPrescriptionReportService prescriptionReportService,
        IReferralRepository referralRepository,
        IObservationRepository observationRepository,
        IObservationReportService observationReportService,
        IPatientHistoryRepository patientHistoryRepository,
        IPatientHistoryReportService patientHistoryReportService,
        IPaymentNameRepository paymentNameRepository,
        IPaymentVoucherRepository paymentVoucherRepository,
        IPaymentReportService paymentReportService,
        IReceiptNameRepository receiptNameRepository,
        IReceiptVoucherRepository receiptVoucherRepository,
        IReceiptReportService receiptReportService,
        IConsolidatedLedgerReportService consolidatedLedgerReportService,
        IAccountingMaintenanceRepository accountingMaintenanceRepository,
        IUserAdministrationRepository userAdministrationRepository,
        SmtpDefaults smtpDefaults)
    {
        this.logDirectory = logDirectory;
        this.backupDirectory = backupDirectory;
        this.maintenanceHistoryFileScanLimit = maintenanceHistoryFileScanLimit;
        this.maintenanceHistoryDefaultMaxRows = maintenanceHistoryDefaultMaxRows;
        this.databasePath = databasePath;
        this.authenticationService = authenticationService;
        this.patientRepository = patientRepository;
        this.appointmentRepository = appointmentRepository;
        this.appointmentReportService = appointmentReportService;
        this.appointmentRdlcExportService = appointmentRdlcExportService;
        this.paymentRdlcExportService = paymentRdlcExportService;
        this.receiptRdlcExportService = receiptRdlcExportService;
        this.consolidatedLedgerRdlcExportService = consolidatedLedgerRdlcExportService;
        this.medicineRepository = medicineRepository;
        this.prescriptionRepository = prescriptionRepository;
        this.prescriptionReportService = prescriptionReportService;
        this.referralRepository = referralRepository;
        this.observationRepository = observationRepository;
        this.observationReportService = observationReportService;
        this.patientHistoryRepository = patientHistoryRepository;
        this.patientHistoryReportService = patientHistoryReportService;
        this.paymentNameRepository = paymentNameRepository;
        this.paymentVoucherRepository = paymentVoucherRepository;
        this.paymentReportService = paymentReportService;
        this.receiptNameRepository = receiptNameRepository;
        this.receiptVoucherRepository = receiptVoucherRepository;
        this.receiptReportService = receiptReportService;
        this.consolidatedLedgerReportService = consolidatedLedgerReportService;
        this.accountingMaintenanceRepository = accountingMaintenanceRepository;
        this.userAdministrationRepository = userAdministrationRepository;
        this.smtpDefaults = smtpDefaults;

        Text = "DoctorSoft - Main";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;

        var backgroundImage = TryLoadLegacyImage("background.jpg");
        ClientSize = backgroundImage?.Size ?? new Size(800, 580);

        var appIcon = TryLoadAppIcon();
        if (appIcon is not null)
        {
            Icon = appIcon;
        }

        var shellCanvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackgroundImage = backgroundImage,
            BackgroundImageLayout = ImageLayout.Stretch
        };

        var submenuHost = new Panel
        {
            Location = new Point(200, 250),
            Size = new Size(255, 300),
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };

        Panel CreateSubmenu(string title, params (string Text, Action Action)[] entries)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };

            var heading = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            panel.Controls.Add(heading);

            var y = 42;
            foreach (var entry in entries)
            {
                var link = new LinkLabel
                {
                    Text = entry.Text,
                    LinkBehavior = LinkBehavior.HoverUnderline,
                    AutoSize = true,
                    Location = new Point(16, y),
                    Font = new Font("Segoe UI", 9, FontStyle.Regular),
                    LinkColor = Color.LightYellow,
                    ActiveLinkColor = Color.FromArgb(255, 240, 170),
                    VisitedLinkColor = Color.LightYellow,
                    BackColor = Color.Transparent
                };
                link.Click += (_, _) => entry.Action();
                panel.Controls.Add(link);
                y += 28;
            }

            return panel;
        }

        var submenus = new Dictionary<string, Panel>
        {
            ["planner"] = CreateSubmenu(
                "Doctor Planner",
                ("Appointment Management", () => { using var form = new AppointmentManagementForm(appointmentRepository, patientRepository); form.ShowDialog(this); }),
                ("Appointment Reports", () => { using var form = new AppointmentReportsForm(appointmentReportService, appointmentRdlcExportService); form.ShowDialog(this); }),
                ("Observations", () => { using var form = new ObservationManagementForm(patientRepository, observationRepository); form.ShowDialog(this); }),
                ("Observation Reports", () => { using var form = new ObservationReportsForm(observationReportService); form.ShowDialog(this); }),
                ("History Reports", () => { using var form = new PatientHistoryReportsForm(patientHistoryReportService); form.ShowDialog(this); })),
            ["record"] = CreateSubmenu(
                "Patient Records",
                ("Patient Management", () => { using var form = new PatientManagementForm(patientRepository); form.ShowDialog(this); }),
                ("Patient History", () => { using var form = new PatientHistoryManagementForm(patientRepository, patientHistoryRepository); form.ShowDialog(this); }),
                ("Referral Management", () => { using var form = new ReferralManagementForm(patientRepository, referralRepository); form.ShowDialog(this); })),
            ["prescription"] = CreateSubmenu(
                "Prescription",
                ("Prescription Management", () => { using var form = new PrescriptionManagementForm(patientRepository, medicineRepository, prescriptionRepository); form.ShowDialog(this); }),
                ("Prescription Reports", () => { using var form = new PrescriptionReportsForm(prescriptionReportService); form.ShowDialog(this); })),
            ["accounts"] = CreateSubmenu(
                "Accounts",
                ("Payments Setup", () => { using var form = new PaymentSetupForm(paymentNameRepository); form.ShowDialog(this); }),
                ("Payment Voucher", () => { using var form = new PaymentVoucherForm(paymentVoucherRepository, paymentNameRepository); form.ShowDialog(this); }),
                ("Payment Reports", () => { using var form = new PaymentReportsForm(paymentReportService, paymentRdlcExportService); form.ShowDialog(this); }),
                ("Receipts Setup", () => { using var form = new ReceiptSetupForm(receiptNameRepository); form.ShowDialog(this); }),
                ("Receipt Voucher", () => { using var form = new ReceiptVoucherForm(receiptVoucherRepository, receiptNameRepository, patientRepository); form.ShowDialog(this); }),
                ("Receipt Reports", () => { using var form = new ReceiptReportsForm(receiptReportService, receiptRdlcExportService); form.ShowDialog(this); }),
                ("Consolidated Ledger", () => { using var form = new ConsolidatedLedgerReportsForm(consolidatedLedgerReportService, consolidatedLedgerRdlcExportService); form.ShowDialog(this); }),
                ("Accounting Maintenance", () =>
                {
                    using var form = new AccountingMaintenanceForm(
                        accountingMaintenanceRepository,
                        userName,
                        logDirectory,
                        maintenanceHistoryFileScanLimit,
                        maintenanceHistoryDefaultMaxRows);
                    form.ShowDialog(this);
                })),
            ["utilities"] = CreateSubmenu(
                "Utilities",
                ("User Administration", () => { using var form = new UserAdministrationForm(userAdministrationRepository, userName); form.ShowDialog(this); }),
                ("Change Password", () => { using var form = new ChangePasswordForm(userName, authenticationService, userAdministrationRepository); form.ShowDialog(this); }),
                ("Database Utilities", () => { using var form = new DatabaseUtilitiesForm(databasePath, backupDirectory); form.ShowDialog(this); }))
        };

        string? activeMenu = null;
        void ShowSubmenu(string key, Rectangle sourceBounds)
        {
            if (activeMenu == key && submenuHost.Visible)
            {
                submenuHost.Visible = false;
                activeMenu = null;
                return;
            }

            var targetX = sourceBounds.Right + 12;
            var targetY = sourceBounds.Top - 6;

            if (targetX + submenuHost.Width > shellCanvas.Width - 8)
            {
                targetX = shellCanvas.Width - submenuHost.Width - 8;
            }

            if (targetY + submenuHost.Height > shellCanvas.Height - 8)
            {
                targetY = shellCanvas.Height - submenuHost.Height - 8;
            }

            targetX = Math.Max(0, targetX);
            targetY = Math.Max(0, targetY);
            submenuHost.Location = new Point(targetX, targetY);

            submenuHost.Controls.Clear();
            submenuHost.Controls.Add(submenus[key]);
            submenuHost.Visible = true;
            activeMenu = key;
        }

        void HideSubmenu()
        {
            submenuHost.Visible = false;
            activeMenu = null;
        }

        Label CreateHotspot(Rectangle bounds, string menuKey)
        {
            var hotspot = new Label
            {
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Bounds = bounds
            };
            hotspot.Click += (_, _) => ShowSubmenu(menuKey, hotspot.Bounds);
            return hotspot;
        }

        Label CreateActionHotspot(Rectangle bounds, Action action)
        {
            var hotspot = new Label
            {
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Bounds = bounds
            };
            hotspot.Click += (_, _) =>
            {
                HideSubmenu();
                action();
            };
            return hotspot;
        }

        void WireOutsideClickDismiss(Control root)
        {
            root.MouseDown += (_, _) =>
            {
                if (!submenuHost.Visible)
                {
                    return;
                }

                var mouse = shellCanvas.PointToClient(Cursor.Position);
                if (!submenuHost.Bounds.Contains(mouse))
                {
                    HideSubmenu();
                }
            };

            foreach (Control child in root.Controls)
            {
                if (child == submenuHost)
                {
                    continue;
                }

                WireOutsideClickDismiss(child);
            }
        }

        shellCanvas.Controls.Add(CreateHotspot(new Rectangle(46, 260, 140, 22), "record"));
        shellCanvas.Controls.Add(CreateHotspot(new Rectangle(46, 288, 140, 22), "planner"));
        shellCanvas.Controls.Add(CreateHotspot(new Rectangle(46, 318, 140, 22), "prescription"));
        shellCanvas.Controls.Add(CreateHotspot(new Rectangle(46, 350, 140, 22), "accounts"));
        shellCanvas.Controls.Add(CreateHotspot(new Rectangle(46, 380, 140, 22), "utilities"));
        shellCanvas.Controls.Add(CreateActionHotspot(new Rectangle(46, 410, 140, 22), () =>
        {
            using var form = new EmailCenterForm(this.smtpDefaults);
            form.ShowDialog(this);
        }));
        shellCanvas.Controls.Add(CreateActionHotspot(new Rectangle(46, 440, 140, 22), () =>
        {
            using var form = new DrugReferenceForm(medicineRepository);
            form.ShowDialog(this);
        }));
        shellCanvas.Controls.Add(CreateActionHotspot(new Rectangle(46, 470, 140, 22), () => Application.Exit()));

        shellCanvas.Controls.Add(submenuHost);
        WireOutsideClickDismiss(shellCanvas);
        Controls.Add(shellCanvas);
    }

    private static Icon? TryLoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "doctorsoft.ico");
        return File.Exists(iconPath) ? new Icon(iconPath) : null;
    }

    private static Image? TryLoadLegacyImage(string fileName)
    {
        var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "LegacyMenu", fileName);
        return File.Exists(imagePath) ? Image.FromFile(imagePath) : null;
    }
}
