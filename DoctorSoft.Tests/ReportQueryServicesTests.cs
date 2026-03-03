using DoctorSoft.Reports.Services;
using DoctorSoft.Tests.TestInfrastructure;

namespace DoctorSoft.Tests;

public class ReportQueryServicesTests
{
    [Fact]
    public async Task AppointmentReportService_HandlesFilters_AndFallbackParsing()
    {
        var factory = new DelegateConnectionFactory((sql, parameters) =>
        {
            if (sql.Contains("FROM [appointment]", StringComparison.Ordinal) && sql.Contains("a.[Start_Date] = ? AND a.[Patient_Name] = ?", StringComparison.Ordinal))
            {
                Assert.Equal("Alice", parameters[1]);
                return
                [
                    Row(("Start_Date", "2026-03-01"), ("App_Time", "09:00"), ("Patient_Name", "Alice"), ("Event_Title", "Consult"), ("Event_Details", "General"), ("Status", 1), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                ];
            }

            if (sql.Contains("FROM [appointment]", StringComparison.Ordinal) && sql.Contains("a.[Start_Date] = ? AND a.[Status] = ?", StringComparison.Ordinal))
            {
                return
                [
                    Row(("Start_Date", "2026-03-01"), ("App_Time", "09:00"), ("Patient_Name", "Alice"), ("Event_Title", "Consult"), ("Event_Details", "General"), ("Status", true), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                ];
            }

            return
            [
                Row(("Start_Date", "2026-03-01"), ("App_Time", "09:00"), ("Patient_Name", "Alice"), ("Event_Title", "Consult"), ("Event_Details", "General"), ("Status", "1"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345")),
                Row(("Start_Date", "bad-date"), ("App_Time", "10:00"), ("Patient_Name", "Bob"), ("Event_Title", "Review"), ("Event_Details", "Follow-up"), ("Status", "unknown"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
            ];
        });

        var service = new AppointmentReportService(factory);

        var byPatient = await service.GetAppointReportAsync(new DateTime(2026, 3, 1), "  Alice  ");
        var completed = await service.GetAllAppByDateReportAsync(new DateTime(2026, 3, 1), completedOnly: true);
        var all = await service.GetAllAppReportAsync();

        Assert.Single(byPatient);
        Assert.True(byPatient[0].Status);
        Assert.Single(completed);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, x => x.PatientName == "Bob" && x.StartDate == DateTime.MinValue && !x.Status);
    }

    [Fact]
    public async Task PaymentReportService_HandlesAllQueries_AndNumericFallbacks()
    {
        var factory = new DelegateConnectionFactory((sql, parameters) =>
        {
            if (sql.Contains("p.[pno] = ?", StringComparison.Ordinal))
            {
                if (Equals(parameters[0], 101))
                {
                    return
                    [
                        Row(("pno", 101), ("pdate", "bad-date"), ("pname", "Supplies"), ("prec", "Vendor B"), ("pby", "Admin"), ("amtpd", "NaN"), ("coex", "Office"), ("pmon", "March"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                    ];
                }

                return
                [
                    Row(("pno", 100), ("pdate", "2026-03-01"), ("pname", "Utilities"), ("prec", "Vendor A"), ("pby", "Admin"), ("amtpd", "250.50"), ("coex", "Power"), ("pmon", "March"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                ];
            }

            if (sql.Contains("p.[pname] = ?", StringComparison.Ordinal))
            {
                Assert.Equal("Utilities", parameters[0]);
            }

            return
            [
                Row(("pno", 100), ("pdate", "2026-03-01"), ("pname", "Utilities"), ("prec", "Vendor A"), ("pby", "Admin"), ("amtpd", "250.50"), ("coex", "Power"), ("pmon", "March"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
            ];
        });

        var service = new PaymentReportService(factory);

        var byVoucher = await service.GetByVoucherNoAsync(100);
        var byName = await service.GetByPaymentNameAsync("  Utilities ");
        var byRange = await service.GetByDateRangeAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        Assert.Single(byVoucher);
        Assert.Single(byName);
        Assert.Single(byRange);
        Assert.Equal(250.50m, byRange[0].AmountPaid);

        var fallback = (await service.GetByVoucherNoAsync(101)).Single();
        Assert.Equal(DateTime.MinValue, fallback.VoucherDate);
        Assert.Equal(0m, fallback.AmountPaid);
    }

    [Fact]
    public async Task ReceiptReportService_HandlesAllQueries_AndNumericFallbacks()
    {
        var factory = new DelegateConnectionFactory((sql, parameters) =>
        {
            if (sql.Contains("r.[rno] = ?", StringComparison.Ordinal))
            {
                if (Equals(parameters[0], 11))
                {
                    return
                    [
                        Row(("rno", 11), ("rdate", "bad-date"), ("rname", "Patient B"), ("lname", "Tests"), ("amtpd", "oops"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                    ];
                }

                return
                [
                    Row(("rno", 10), ("rdate", "2026-03-01"), ("rname", "Patient A"), ("lname", "Consultation"), ("amtpd", "500.00"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                ];
            }

            if (sql.Contains("r.[lname] = ?", StringComparison.Ordinal))
            {
                Assert.Equal("Consultation", parameters[0]);
            }

            return
            [
                Row(("rno", 10), ("rdate", "2026-03-01"), ("rname", "Patient A"), ("lname", "Consultation"), ("amtpd", "500.00"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
            ];
        });

        var service = new ReceiptReportService(factory);

        var byVoucher = await service.GetByVoucherNoAsync(10);
        var byLedger = await service.GetByLedgerNameAsync("  Consultation ");
        var byRange = await service.GetByDateRangeAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        Assert.Single(byVoucher);
        Assert.Single(byLedger);
        Assert.Single(byRange);

        var fallback = (await service.GetByVoucherNoAsync(11)).Single();
        Assert.Equal(DateTime.MinValue, fallback.VoucherDate);
        Assert.Equal(0m, fallback.AmountReceived);
    }

    [Fact]
    public async Task ConsolidatedLedgerReportService_ComputesRunningBalance_AndLedgerFilter()
    {
        var factory = new DelegateConnectionFactory((sql, parameters) =>
        {
            if (sql.Contains("l.[ldate] BETWEEN ? AND ? AND l.[lname] = ?", StringComparison.Ordinal))
            {
                if (Equals(parameters[2], "Lab"))
                {
                    return
                    [
                        Row(("autoid", 3), ("ldate", "bad-date"), ("lno", 7), ("lname", "Lab"), ("debit", "x"), ("credit", "y"), ("coex", "Third"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                    ];
                }

                Assert.Equal("Consultation", parameters[2]);
                return
                [
                    Row(("autoid", 1), ("ldate", "2026-03-01"), ("lno", 5), ("lname", "Consultation"), ("debit", "0"), ("credit", "200"), ("coex", "First"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345")),
                    Row(("autoid", 2), ("ldate", "2026-03-02"), ("lno", 6), ("lname", "Consultation"), ("debit", "50"), ("credit", "0"), ("coex", "Second"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                ];
            }

            return
            [
                Row(("autoid", 1), ("ldate", "2026-03-01"), ("lno", 5), ("lname", "Consultation"), ("debit", "0"), ("credit", "200"), ("coex", "First"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345")),
                Row(("autoid", 2), ("ldate", "2026-03-02"), ("lno", 6), ("lname", "Consultation"), ("debit", "50"), ("credit", "0"), ("coex", "Second"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
            ];
        });

        var service = new ConsolidatedLedgerReportService(factory);

        var byRange = await service.GetByDateRangeAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
        var byRangeAndLedger = await service.GetByDateRangeAndLedgerAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), "  Consultation ");

        Assert.Equal(2, byRange.Count);
        Assert.Equal(2, byRangeAndLedger.Count);
        Assert.Equal(200m, byRangeAndLedger[0].RunningBalance);
        Assert.Equal(150m, byRangeAndLedger[1].RunningBalance);

        var fallback = (await service.GetByDateRangeAndLedgerAsync(DateTime.MinValue, DateTime.MaxValue, "Lab")).Single();
        Assert.Equal(DateTime.MinValue, fallback.Date);
        Assert.Equal(0m, fallback.Debit);
        Assert.Equal(0m, fallback.Credit);
    }

    [Fact]
    public async Task ObservationReportService_HandlesAllQueries_AndAgeFallback()
    {
        var factory = new DelegateConnectionFactory((sql, parameters) =>
        {
            if (sql.Contains("o.[Date] = ? AND o.[Patient_Name] = ?", StringComparison.Ordinal))
            {
                Assert.Equal("Alice", parameters[1]);
            }

            if (sql.Contains("o.[Patient_Name] = ?", StringComparison.Ordinal) && Equals(parameters[0], "Bob"))
            {
                return
                [
                    Row(("Date", "bad-date"), ("Time", "09:00"), ("Patient_Name", "Bob"), ("Age", "unknown"), ("Sex", "M"), ("problem", "Fever"), ("Observation", "Rest"), ("testsrecom", "CBC"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                ];
            }

            return
            [
                Row(("Date", "2026-03-01"), ("Time", "08:00"), ("Patient_Name", "Alice"), ("Age", "34"), ("Sex", "F"), ("problem", "Headache"), ("Observation", "Observe"), ("testsrecom", "Blood"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
            ];
        });

        var service = new ObservationReportService(factory);

        var byDate = await service.GetByDateAsync(new DateTime(2026, 3, 1));
        var byDateAndPatient = await service.GetByDateAndPatientAsync(new DateTime(2026, 3, 1), " Alice ");
        var byPatient = await service.GetByPatientAsync("Alice");

        Assert.Single(byDate);
        Assert.Single(byDateAndPatient);
        Assert.Single(byPatient);

        var fallback = (await service.GetByPatientAsync("Bob")).Single();
        Assert.Equal(DateTime.MinValue, fallback.Date);
        Assert.Null(fallback.Age);
    }

    [Fact]
    public async Task PatientHistoryReportService_MapsRows()
    {
        var factory = new DelegateConnectionFactory((_, parameters) =>
        {
            Assert.Equal("Alice", parameters[0]);
            return
            [
                Row(("Patient_Name", "Alice"), ("Test_Date", "2026-03-01"), ("Test_Name", "CBC"), ("Test_Description", "Blood count"), ("Observations", "Normal"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
            ];
        });

        var service = new PatientHistoryReportService(factory);
        var rows = await service.GetByPatientAsync(" Alice ");

        Assert.Single(rows);
        Assert.Equal("Alice", rows[0].PatientName);
        Assert.Equal("CBC", rows[0].TestName);
    }

    [Fact]
    public async Task PrescriptionReportService_HandlesAllQueries_AndOptionalPatientFilter()
    {
        var factory = new DelegateConnectionFactory((sql, parameters) =>
        {
            if (sql.Contains("pm.[Presc_Id] = ?", StringComparison.Ordinal) && Equals(parameters[0], 2))
            {
                return
                [
                    Row(("Presc_Id", 2), ("Patient_Name", "Bob"), ("Patient_Address", "Addr 2"), ("Patient_Age", "bad-age"), ("Date", "bad-date"), ("Time", "11:00"), ("Medicine", "Med B"), ("Type", "Syrup"), ("Dosage", "5ml"), ("Quantity", "1"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
                ];
            }

            if (sql.Contains("pm.[Patient_Name] = ?", StringComparison.Ordinal))
            {
                Assert.Equal("Alice", parameters[^1]);
            }

            return
            [
                Row(("Presc_Id", 1), ("Patient_Name", "Alice"), ("Patient_Address", "Addr 1"), ("Patient_Age", "30"), ("Date", "2026-03-01"), ("Time", "10:00"), ("Medicine", "Med A"), ("Type", "Tablet"), ("Dosage", "1-0-1"), ("Quantity", "10"), ("ClinicName", "Clinic"), ("DoctorName", "Dr Test"), ("ClinicAddr", "Addr"), ("ClinicPhone", "12345"))
            ];
        });

        var service = new PrescriptionReportService(factory);

        var byId = await service.GetByPrescriptionIdAsync(1);
        var byPatient = await service.GetByPatientAsync(" Alice ");
        var byDate = await service.GetByDateAsync(new DateTime(2026, 3, 1));
        var byDateAndPatient = await service.GetByDateAsync(new DateTime(2026, 3, 1), "Alice");

        Assert.Single(byId);
        Assert.Single(byPatient);
        Assert.Single(byDate);
        Assert.Single(byDateAndPatient);

        var fallback = (await service.GetByPrescriptionIdAsync(2)).Single();
        Assert.Equal(DateTime.MinValue, fallback.Date);
        Assert.Null(fallback.PatientAge);
    }

    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] items)
    {
        return items.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }
}
