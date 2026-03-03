# Cutover and Hypercare Runbook

## Phase objective
Shift daily operations from VB6 to C# with rollback safety and controlled monitoring.

## Roles
- Release owner
- App engineer
- Data/DB owner
- QA owner
- Business approver

## T-3 days
1. Freeze DB schema changes.
2. Run full parity pack from `REPORT_PARITY_VERIFICATION_PACK.md`.
3. Confirm release build generated via `scripts/publish-release.ps1`.
4. Confirm backup folder and log folder write permissions on target machines.

## T-1 day
1. Backup production database (`.mdb`).
2. Capture baseline metrics:
   - User login success rate
   - Critical report generation success
   - Mean response time for patient search and appointment load
3. Share go-live communication and support contacts.

## Go-live day
1. Distribute release artifact (`release/publish`).
2. Launch C# app and run smoke suite:
   - Login
   - Patient search/add/update
   - Appointment report load and PDF export
   - Payment/Receipt report load and PDF export
   - Consolidated ledger report load and PDF export
   - User administration and password change
   - Database backup utility
3. Capture first-hour logs and scan for errors.

## Rollback criteria
Rollback to VB6 if any of the following occurs:
- Critical workflow unavailable > 30 minutes
- Data integrity risk confirmed
- Financial totals mismatch without immediate workaround

## Rollback steps
1. Stop C# app usage.
2. Restore latest verified DB backup if data inconsistency detected.
3. Resume VB6 operations.
4. Raise incident and create corrective action plan.

## Hypercare window (first 2 weeks)
- Daily triage at fixed time.
- Track incidents by severity:
  - Sev1: workflow blocked
  - Sev2: major degradation
  - Sev3: minor defect
- Weekly patch release if required.

## Hypercare dashboard fields
- Date
- Total incidents
- Sev1/Sev2/Sev3 counts
- Mean time to resolution
- Open blockers

## Exit criteria from hypercare
- No Sev1 issues for 7 consecutive days.
- No unresolved financial/report parity defects.
- Business approver sign-off.
