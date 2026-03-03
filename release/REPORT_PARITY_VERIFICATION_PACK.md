# Report Parity Verification Pack

## Objective
Validate business parity between VB6 and C# report outputs for migrated report modules.

## Scope (Current)
- Appointment reports (`Appoint`, `AllAppDt`, `AllApp`)
- Payment reports
- Receipt reports
- Consolidated ledger report

## Prerequisites
1. Same database snapshot used for both VB6 and C# runs.
2. C# build from `DoctorSoft.CSharp/release/publish`.
3. VB6 executable available for side-by-side checks.
4. Test data includes:
   - Completed and pending appointments
   - Payment and receipt vouchers
   - Ledger debit/credit entries across date ranges

## Verification matrix
| Report | Filter mode | Expected parity checks |
|---|---|---|
| Appointment | Date + optional patient | Row count, patient/time ordering, status text |
| Appointment | Date completed only | Completed-only filter accuracy |
| Appointment | All | Global row count and date ordering |
| Payment | Voucher no | Voucher identity, amount, receiver, cause |
| Payment | Payment name | Filter accuracy and totals |
| Payment | Date range | Range inclusion/exclusion and totals |
| Receipt | Voucher no | Voucher identity, ledger, amount |
| Receipt | Ledger name | Filter accuracy and totals |
| Receipt | Date range | Range inclusion/exclusion and totals |
| Consolidated Ledger | Date range | Debit/credit totals, running balance |
| Consolidated Ledger | Date + ledger | Ledger filter and running balance |

## Step-by-step execution
1. Run report in VB6 with fixed filter inputs.
2. Run matching report in C# with same inputs.
3. Export C# PDF for evidence.
4. Capture row count and totals from both systems.
5. Record result in `release/templates/report-parity-results.csv`.
6. Mark each row as `PASS` or `FAIL`.

## Acceptance criteria
- All rows in parity result template are `PASS`.
- No mismatch in row counts.
- No mismatch in financial totals (`debit`, `credit`, `amount` fields).
- No filter behavior mismatch for selected date/name/voucher criteria.

## Defect logging format
- Report name:
- Filter mode:
- Input parameters:
- VB6 output summary:
- C# output summary:
- Difference observed:
- Severity:
- Proposed fix:

## Sign-off
- QA owner:
- Business approver:
- Date:
