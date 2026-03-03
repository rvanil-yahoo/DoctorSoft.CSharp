using System.Data.Odbc;
using DoctorSoft.Data.Access;
using DoctorSoft.Data.Repositories;
using DoctorSoft.Domain.Models;
using DoctorSoft.Tests.TestInfrastructure;

namespace DoctorSoft.Tests;

public class DataRepositoriesTests
{
    [Fact]
    public void AccessConnectionFactory_ValidatesPath_AndCreatesOdbcConnection()
    {
        Assert.Throws<ArgumentException>(() => new AccessConnectionFactory(" "));

        var factory = new AccessConnectionFactory("d:\\db.mdb");
        var connection = factory.Create();

        Assert.IsType<OdbcConnection>(connection);
    }

    [Fact]
    public async Task AccessAppointmentRepository_ExecutesSearchAndMutations()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (sql, _) =>
            {
                if (sql.Contains("SELECT [Date_Added]", StringComparison.Ordinal))
                {
                    return
                    [
                        Row(("Date_Added", "2026-03-01"), ("Start_Date", "bad"), ("Event_Title", "Consult"), ("Event_Details", "Details"), ("App_Time", "10:00"), ("Patient_Name", "Alice"), ("Patient_Address", "Addr"), ("Patient_Age", "unknown"), ("Patient_Sex", "F"), ("Status", "1"))
                    ];
                }

                return Array.Empty<IReadOnlyDictionary<string, object?>>();
            },
            ScalarHandler = (_, _) => 1
        };

        var repository = new AccessAppointmentRepository(new DelegateConnectionFactory(behavior));

        var search = await repository.SearchAsync(DateTime.Today, pendingOnly: true, " Alice ");
        var exists = await repository.ExistsAsync(DateTime.Today, " Alice ");

        await repository.AddAsync(new Appointment
        {
            StartDate = DateTime.Today,
            EventTitle = " Visit ",
            EventDetails = " Details ",
            AppTime = " 10:00 ",
            PatientName = " Alice ",
            PatientAddress = " Addr ",
            PatientSex = " F ",
            Status = false
        });

        await repository.MarkCompletedAsync(DateTime.Today, " Alice ", " 10:00 ");

        Assert.Single(search);
        Assert.Equal(DateTime.MinValue, search[0].StartDate);
        Assert.Null(search[0].PatientAge);
        Assert.True(search[0].Status);
        Assert.True(exists);
        Assert.Contains(behavior.Commands, c => c.Kind == "NonQuery" && c.CommandText.StartsWith("INSERT INTO [appointment]", StringComparison.Ordinal));
        Assert.Contains(behavior.Commands, c => c.Kind == "NonQuery" && c.CommandText.StartsWith("UPDATE [appointment]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AccessMedicineRepository_ReadsRowsAndByName()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (sql, parameters) =>
            {
                if (sql.Contains("WHERE [Medicine] = ?", StringComparison.Ordinal))
                {
                    if (Equals(parameters[0], "Unknown"))
                    {
                        return Array.Empty<IReadOnlyDictionary<string, object?>>();
                    }

                    return [Row(("Medicine", "Paracetamol"), ("Type", "Tablet"))];
                }

                return [Row(("Medicine", "Paracetamol"), ("Type", "Tablet"))];
            }
        };

        var repository = new AccessMedicineRepository(new DelegateConnectionFactory(behavior));

        var all = await repository.GetAllAsync();
        var found = await repository.GetByNameAsync(" Paracetamol ");
        var missing = await repository.GetByNameAsync("Unknown");

        Assert.Single(all);
        Assert.NotNull(found);
        Assert.Null(missing);
    }

    [Fact]
    public async Task AccessObservationRepository_CoversAllOperations()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (sql, _) =>
            {
                if (sql.Contains("FROM [patient]", StringComparison.Ordinal))
                {
                    return [Row(("Patient_age", "40"), ("Patient_sex", "M"))];
                }

                if (sql.Contains("FROM [observations]", StringComparison.Ordinal))
                {
                    return [Row(("Date", "bad"), ("Time", "09:00"), ("Patient_Name", "Alice"), ("Age", "x"), ("Sex", "F"), ("problem", "P"), ("Observation", "O"), ("testsrecom", "T"))];
                }

                return Array.Empty<IReadOnlyDictionary<string, object?>>();
            },
            ScalarHandler = (_, _) => 1
        };

        var repository = new AccessObservationRepository(new DelegateConnectionFactory(behavior));

        var list = await repository.SearchAsync("Alice", DateTime.Today);
        var draft = await repository.BuildDraftForPatientAsync(" Alice ");
        var exists = await repository.ExistsAsync("Alice", DateTime.Today);

        await repository.AddAsync(new Observation { Date = DateTime.Today, Time = "10:00", PatientName = "Alice", Sex = "F", Problem = "P", ObservationText = "O", TestsRecommended = "T" });
        await repository.UpdateAsync(new Observation { Date = DateTime.Today, Time = "10:05", PatientName = "Alice", Sex = "F", Problem = "P2", ObservationText = "O2", TestsRecommended = "T2" }, "Alice", DateTime.Today);
        await repository.DeleteAsync("Alice", DateTime.Today);

        Assert.Single(list);
        Assert.Equal(DateTime.Today, draft?.Date);
        Assert.True(exists);
    }

    [Fact]
    public async Task AccessPatientHistoryRepository_CoversAllOperations()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (_, _) => [Row(("Patient_Name", "Alice"), ("Test_Date", "bad"), ("Test_Name", "CBC"), ("Test_Description", "Desc"), ("Observations", "Obs"))],
            ScalarHandler = (_, _) => 1
        };

        var repository = new AccessPatientHistoryRepository(new DelegateConnectionFactory(behavior));

        var rows = await repository.SearchAsync("Alice", DateTime.Today);
        var exists = await repository.ExistsAsync("Alice", DateTime.Today);
        await repository.AddAsync(new PatientHistoryEntry { PatientName = "Alice", TestDate = DateTime.Today, TestName = "CBC", TestDescription = "Desc", Observations = "Obs" });
        await repository.UpdateAsync(new PatientHistoryEntry { PatientName = "Alice", TestDate = DateTime.Today, TestName = "CBC", TestDescription = "Desc", Observations = "Obs" }, "Alice", DateTime.Today);
        await repository.DeleteAsync("Alice", DateTime.Today);

        Assert.Single(rows);
        Assert.Equal(DateTime.Today, rows[0].TestDate);
        Assert.True(exists);
    }

    [Fact]
    public async Task AccessPatientRepository_CoversSearchAndMutations()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (sql, _) =>
            {
                if (sql.Contains("WHERE [Patient_Name] = ?", StringComparison.Ordinal))
                {
                    return [Row(("Patient_Name", "Alice"), ("Patient_address", "Addr"), ("Patient_Phone", "1"), ("Patient_age", "bad"), ("Patient_sex", "F"), ("bg", "O+"))];
                }

                return [Row(("Patient_Name", "Alice"), ("Patient_address", "Addr"), ("Patient_Phone", "1"), ("Patient_age", "30"), ("Patient_sex", "F"), ("bg", "O+"))];
            }
        };

        var repository = new AccessPatientRepository(new DelegateConnectionFactory(behavior));

        var search = await repository.SearchAsync("Alice", "Addr", "O+");
        var byName = await repository.GetByNameAsync("Alice");
        await repository.AddAsync(new Patient { Name = "Alice", Address = "Addr", Phone = "1", Age = 30, Sex = "F", BloodGroup = "O+" });
        await repository.UpdateAsync(new Patient { Name = "Alice", Address = "Addr2", Phone = "2", Age = null, Sex = "F", BloodGroup = "O+" }, "Alice");

        Assert.Single(search);
        Assert.NotNull(byName);
        Assert.Null(byName!.Age);
    }

    [Fact]
    public async Task AccessPaymentNameRepository_CoversAllOperations()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (_, _) => [Row(("payname", "Utilities"))],
            ScalarHandler = (_, _) => 1
        };

        var repository = new AccessPaymentNameRepository(new DelegateConnectionFactory(behavior));

        var all = await repository.GetAllAsync();
        var exists = await repository.ExistsAsync("Utilities");
        await repository.AddAsync(" Utilities ");
        await repository.RenameAsync("Utilities", "Office");
        await repository.DeleteAsync("Office");

        Assert.Single(all);
        Assert.True(exists);
    }

    [Fact]
    public async Task AccessPaymentVoucherRepository_HandlesSuccessAndRollback()
    {
        var successBehavior = new DelegateDbBehavior
        {
            ScalarHandler = (sql, _) =>
                sql.StartsWith("SELECT MAX([pno])", StringComparison.Ordinal)
                    ? DBNull.Value
                    : 1
        };
        var successRepository = new AccessPaymentVoucherRepository(new DelegateConnectionFactory(successBehavior));

        var next = await successRepository.GetNextVoucherNoAsync();
        await successRepository.AddAsync(new PaymentVoucher
        {
            VoucherNo = 1,
            VoucherDate = new DateTime(2026, 3, 1),
            PaidTowards = "Utilities",
            ReceiverName = "Vendor",
            PaidBy = "Admin",
            AmountPaid = 10m,
            ExpenditureCause = "Test"
        });

        Assert.Equal(1, next);
        Assert.Equal(1, successBehavior.CommittedTransactions);

        var failBehavior = new DelegateDbBehavior
        {
            NonQueryHandler = (sql, _) =>
            {
                if (sql.StartsWith("INSERT INTO [ledger]", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("boom");
                }

                return 1;
            }
        };

        var failRepository = new AccessPaymentVoucherRepository(new DelegateConnectionFactory(failBehavior));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failRepository.AddAsync(new PaymentVoucher
        {
            VoucherNo = 2,
            VoucherDate = new DateTime(2026, 3, 1),
            PaidTowards = "Utilities",
            ReceiverName = "Vendor",
            PaidBy = "Admin",
            AmountPaid = 10m,
            ExpenditureCause = "Test"
        }));

        Assert.Equal(1, failBehavior.RolledBackTransactions);
    }

    [Fact]
    public async Task AccessPrescriptionRepository_HandlesSaveAndRollback()
    {
        var successBehavior = new DelegateDbBehavior
        {
            ScalarHandler = (sql, _) =>
                sql.StartsWith("SELECT MAX([Presc_Id])", StringComparison.Ordinal)
                    ? DBNull.Value
                    : 1
        };
        var repository = new AccessPrescriptionRepository(new DelegateConnectionFactory(successBehavior));

        Assert.Equal(1, await repository.GetNextPrescriptionIdAsync());
        Assert.True(await repository.ExistsForPatientAndDateAsync("Alice", DateTime.Today));

        await repository.SaveAsync(new Prescription
        {
            PrescId = 1,
            PatientName = "Alice",
            PatientAddress = "Addr",
            PatientAge = 30,
            Date = DateTime.Today,
            Time = "10:00",
            Lines = new List<PrescriptionLine>
            {
                new() { PrescId = 1, Medicine = "Med", Type = "Tab", Dosage = "1-0-1", Quantity = "10" }
            }
        });

        Assert.Equal(1, successBehavior.CommittedTransactions);

        var failBehavior = new DelegateDbBehavior
        {
            NonQueryHandler = (sql, _) =>
            {
                if (sql.StartsWith("INSERT INTO [Presc_Ref]", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("boom");
                }

                return 1;
            }
        };

        var failRepository = new AccessPrescriptionRepository(new DelegateConnectionFactory(failBehavior));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failRepository.SaveAsync(new Prescription
        {
            PrescId = 1,
            PatientName = "Alice",
            PatientAddress = "Addr",
            Date = DateTime.Today,
            Time = "10:00",
            Lines = new List<PrescriptionLine> { new() { PrescId = 1, Medicine = "Med", Type = "Tab", Dosage = "1", Quantity = "1" } }
        }));

        Assert.Equal(1, failBehavior.RolledBackTransactions);
    }

    [Fact]
    public async Task AccessReceiptNameRepository_CoversAllOperations()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (sql, parameters) =>
            {
                if (sql.Contains("WHERE [RecName] = ?", StringComparison.Ordinal) && Equals(parameters[0], "Missing"))
                {
                    return Array.Empty<IReadOnlyDictionary<string, object?>>();
                }

                return [Row(("RecName", "Consultation"), ("RecPat", "1"))];
            },
            ScalarHandler = (_, _) => 1
        };

        var repository = new AccessReceiptNameRepository(new DelegateConnectionFactory(behavior));

        var all = await repository.GetAllAsync();
        var exists = await repository.ExistsAsync("Consultation");
        await repository.AddAsync("Consultation", true);
        await repository.UpdateAsync("Consultation", "Consult", false);
        await repository.DeleteAsync("Consult");
        var found = await repository.GetByNameAsync("Consultation");
        var missing = await repository.GetByNameAsync("Missing");

        Assert.Single(all);
        Assert.True(all[0].RequiresPatientSelection);
        Assert.True(exists);
        Assert.NotNull(found);
        Assert.Null(missing);
    }

    [Fact]
    public async Task AccessReceiptVoucherRepository_HandlesSuccessAndRollback()
    {
        var successBehavior = new DelegateDbBehavior { ScalarHandler = (_, _) => DBNull.Value };
        var successRepository = new AccessReceiptVoucherRepository(new DelegateConnectionFactory(successBehavior));

        Assert.Equal(1, await successRepository.GetNextVoucherNoAsync());
        await successRepository.AddAsync(new ReceiptVoucher
        {
            VoucherNo = 1,
            VoucherDate = DateTime.Today,
            ReceiverName = "Alice",
            LedgerName = "Consultation",
            AmountReceived = 100m
        });

        Assert.Equal(1, successBehavior.CommittedTransactions);

        var failBehavior = new DelegateDbBehavior
        {
            NonQueryHandler = (sql, _) =>
            {
                if (sql.StartsWith("INSERT INTO [ledger]", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("boom");
                }

                return 1;
            }
        };

        var failRepository = new AccessReceiptVoucherRepository(new DelegateConnectionFactory(failBehavior));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failRepository.AddAsync(new ReceiptVoucher
        {
            VoucherNo = 2,
            VoucherDate = DateTime.Today,
            ReceiverName = "Alice",
            LedgerName = "Consultation",
            AmountReceived = 100m
        }));

        Assert.Equal(1, failBehavior.RolledBackTransactions);
    }

    [Fact]
    public async Task AccessReferralRepository_CoversAllOperations()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (sql, parameters) =>
            {
                if (sql.Contains("FROM [patient]", StringComparison.Ordinal))
                {
                    if (Equals(parameters[0], "Missing"))
                    {
                        return Array.Empty<IReadOnlyDictionary<string, object?>>();
                    }

                    return [Row(("Patient_address", "Addr"), ("Patient_age", "bad"), ("Patient_sex", "F"))];
                }

                if (sql.Contains("FROM [doctordetails]", StringComparison.Ordinal))
                {
                    return [Row(("doctorname", "Dr"), ("clinicname", "Clinic"), ("clinicaddr", "Addr"))];
                }

                if (sql.Contains("FROM [refferral]", StringComparison.Ordinal))
                {
                    return [Row(("refdate", "bad"), ("pname", "Alice"), ("paddr", "Addr"), ("page", "x"), ("psex", "F"), ("fdoc", "Dr"), ("fclin", "Clinic"), ("fclinaddr", "Addr"), ("todoc", "Dr2"), ("toclin", "Clinic2"), ("toaddr", "Addr2"), ("message", "Msg"))];
                }

                return Array.Empty<IReadOnlyDictionary<string, object?>>();
            },
            ScalarHandler = (_, _) => 1
        };

        var repository = new AccessReferralRepository(new DelegateConnectionFactory(behavior));

        var rows = await repository.SearchAsync("Alice", "Dr2");
        var draft = await repository.BuildDraftForPatientAsync("Alice");
        var noDraft = await repository.BuildDraftForPatientAsync("Missing");
        var exists = await repository.ExistsAsync("Alice", DateTime.Today);

        await repository.AddAsync(new Referral { RefDate = DateTime.Today, PatientName = "Alice", PatientAddress = "Addr", PatientSex = "F", FromDoctor = "Dr", FromClinic = "Clinic", FromClinicAddress = "Addr", ToDoctor = "Dr2", ToClinic = "Clinic2", ToAddress = "Addr2", Message = "Msg" });
        await repository.UpdateAsync(new Referral { RefDate = DateTime.Today, PatientName = "Alice", PatientAddress = "Addr", PatientSex = "F", FromDoctor = "Dr", FromClinic = "Clinic", FromClinicAddress = "Addr", ToDoctor = "Dr2", ToClinic = "Clinic2", ToAddress = "Addr2", Message = "Msg" }, "Alice", DateTime.Today);
        await repository.DeleteAsync("Alice", DateTime.Today);

        Assert.Single(rows);
        Assert.Equal(DateTime.Today, rows[0].RefDate);
        Assert.NotNull(draft);
        Assert.Null(noDraft);
        Assert.True(exists);
    }

    [Fact]
    public async Task AccessUserAdministrationRepository_CoversAllOperations()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (_, _) => [Row(("uname", "admin"))]
        };

        var repository = new AccessUserAdministrationRepository(new DelegateConnectionFactory(behavior));

        var users = await repository.GetUsersAsync();
        await repository.AddUserAsync(" admin ", "pass");
        await repository.UpdatePasswordAsync("admin", "newpass");
        await repository.DeleteUserAsync("admin");

        Assert.Single(users);
        var addCommand = Assert.Single(behavior.Commands.Where(c => c.Kind == "NonQuery" && c.CommandText.StartsWith("INSERT INTO [un]", StringComparison.Ordinal)));
        Assert.Contains("$", addCommand.Parameters[1]?.ToString());
    }

    [Fact]
    public async Task AccessUserCredentialStore_FindsOrReturnsNull()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (_, parameters) =>
            {
                if (Equals(parameters[0], "missing"))
                {
                    return Array.Empty<IReadOnlyDictionary<string, object?>>();
                }

                return [Row(("uname", "admin"), ("password", "encoded"))];
            }
        };

        var repository = new AccessUserCredentialStore(new DelegateConnectionFactory(behavior));

        var found = await repository.FindByUserNameAsync("admin");
        var missing = await repository.FindByUserNameAsync("missing");

        Assert.NotNull(found);
        Assert.Equal("admin", found!.UserName);
        Assert.Null(missing);
    }

    [Fact]
    public async Task AccessAccountingMaintenanceRepository_CoversQueriesMutationsAndValidation()
    {
        var behavior = new DelegateDbBehavior
        {
            ReaderHandler = (sql, parameters) =>
            {
                if (sql.Contains("FROM [payment]", StringComparison.Ordinal))
                {
                    return [Row(("pno", "10"), ("pdate", "bad"), ("pname", "Utilities"), ("prec", "Vendor"), ("pby", "Admin"), ("amtpd", "x"), ("coex", "Cause"))];
                }

                if (sql.Contains("FROM [reciepts]", StringComparison.Ordinal))
                {
                    return [Row(("rno", "11"), ("rdate", "bad"), ("rname", "Patient"), ("lname", "Consult"), ("amtpd", "x"))];
                }

                if (sql.Contains("FROM [ledger]", StringComparison.Ordinal) && sql.Contains("SELECT [autoid]", StringComparison.Ordinal))
                {
                    return [Row(("autoid", "1"), ("lno", "10"), ("ldate", "bad"), ("lname", "Consult"), ("debit", "x"), ("credit", "y"), ("coex", "Narr"))];
                }

                return Array.Empty<IReadOnlyDictionary<string, object?>>();
            }
        };

        var repository = new AccessAccountingMaintenanceRepository(new DelegateConnectionFactory(behavior));

        var payments = await repository.GetPaymentsAsync(DateTime.Today, DateTime.Today, "Consult");
        var receipts = await repository.GetReceiptsAsync(DateTime.Today, DateTime.Today, "Consult");
        var ledger = await repository.GetLedgerEntriesAsync(DateTime.Today, DateTime.Today, "Consult");

        await repository.UpdatePaymentVoucherAsync(new PaymentMaintenanceRecord { VoucherNo = 10, VoucherDate = DateTime.Today, PaidTowards = "Utilities", ReceiverName = "Vendor", PaidBy = "Admin", AmountPaid = 10m, ExpenditureCause = "Cause" });
        await repository.UpdateReceiptVoucherAsync(new ReceiptMaintenanceRecord { VoucherNo = 11, VoucherDate = DateTime.Today, ReceiverName = "Patient", LedgerName = "Consult", AmountReceived = 12m });
        await repository.UpdateLedgerEntryAsync(new LedgerMaintenanceRecord { AutoId = 1, Date = DateTime.Today, LedgerName = "Consult", Debit = 12m, Credit = 0m, Narration = "N" });

        await repository.DeletePaymentVoucherAsync(10);
        await repository.DeleteReceiptVoucherAsync(11);
        await repository.DeleteLedgerEntryAsync(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateLedgerEntryAsync(new LedgerMaintenanceRecord { AutoId = 1, Date = DateTime.Today, LedgerName = "", Debit = 1m, Credit = 1m }));

        Assert.Single(payments);
        Assert.Equal(DateTime.MinValue, payments[0].VoucherDate);
        Assert.Single(receipts);
        Assert.Single(ledger);
        Assert.Equal(4, behavior.CommittedTransactions);
    }

    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] items)
    {
        return items.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }
}
