# DoctorSoft C# Migration Backlog Worksheet

## Sizing legend
- XS: 1–2 days
- S: 3–5 days
- M: 1–2 weeks
- L: 2–3 weeks
- XL: 3+ weeks

## Priority backlog (execution order)

| Priority | Module | VB6 Artifacts | Primary Tables | Report Scope | Estimate | Dependency | Definition of Done |
|---|---|---|---|---|---|---|---|
| P0 | Platform foundation | `frmLogin.frm`, `MainFr.frm`, `valida.bas` | `un`, `DoctorDetails` | None | L | None | App starts, login works, shell navigation works, logging + config enabled |
| P1 | Patient master/search | `patnew.frm`, `patsear.frm` | `patient` | None | M | P0 | CRUD + search parity, validations pass, UAT script signed |
| P2 | Appointment lifecycle | `appnew.frm`, `appupd.frm`, `appprn.frm`, `appprndt.frm` | `appointment`, `patient` | `appoint.*`, `allapp.*`, `allappdt.*` | L | P1 | Appointment create/update/query complete, date filtering parity, report outputs accepted |
| P3 | Prescription entry + query | `Prescription.frm`, `qry.frm`, `qrygeneric.frm`, `Quick Search.frm`, `medsearchfrm.frm` | `Presc_Main`, `Presc_Ref`, `drugs`, `medicine`, `patient` | Partial (`medsearchrpt.*`) | XL | P1 | Prescription creation + medicine lookup parity, SQL parameterized, no critical defects |
| P4 | Prescription reporting | `FrmPrescriptionsRpt.frm`, `PatwisePrescFrm.frm`, `datewisefrm.frm` | `Presc_Main`, `Presc_Ref`, `DoctorDetails`, `patient` | `PrescriptionsRpt*.*`, `datewiseprescriptionsreport.*`, `dprescrpt.*`, `reportondrugs.*` | XL | P3 | RDLC outputs approved against VB6 baselines |
| P5 | Referral module | `refferal.frm`, `reffind.frm`, `reffmodi.frm`, `reffdel.frm`, `frmReferralModNew.frm`, `frmReferralDelNew.frm` | `refferral`, `patient`, `DoctorDetails` | `refferralrpt.*` | L | P1 | Add/search/modify/delete parity and referral report sign-off |
| P6 | Observations + patient history | `observations.frm`, `patobservationsfrm.frm`, `phist.frm` | `observations`, `history`, `patient` | `Patobservationsrpt.*`, `pathist.*` | L | P1 | Date-sensitive flows validated, all report filters accurate |
| P7 | Accounts and ledger | `Accpay.frm`, `accpaydt.frm`, `accpaynm.frm`, `accname.frm`, `accvouch.frm`, `accrepad.frm`, `acccons.frm`, `payadd.frm`, `clasadd.frm` | `payment`, `reciepts`, `ledger`, `payname`, `recname`, `DoctorDetails` | `paydt.*`, `payname.*`, `payvouch.*`, `recdt.*`, `recname.*`, `recvouch.*`, `consol.*` | XL | P0 | Financial totals verified, vouchers/ledgers match legacy outputs |
| P8 | Admin + utility | `passfrm.frm`, `Usernew.frm`, `delusr.frm`, `utildoc.frm`, backup/restore forms | `un`, `DoctorDetails` | `Patrep.*` (if retained) | M | P0, P1 | User admin parity, utility settings stable, installer-ready |

## Sprint template (copy per sprint)
- Goal:
- Scope (modules):
- Stories:
- Risks:
- Test/UAT scripts:
- Exit criteria:

## Ownership template
- App/UI owner:
- Data/repository owner:
- Reports owner:
- QA owner:
- Business approver:
