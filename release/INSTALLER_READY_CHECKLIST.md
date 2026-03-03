# DoctorSoft Installer-Ready Checklist

## Build and publish
1. Run:
   - `pwsh ./scripts/publish-release.ps1 -Configuration Release -Runtime win-x64`
2. Confirm publish output exists at `release/publish`.

## Configuration checks
1. Open `release/publish/appsettings.json`.
2. Verify:
   - `Database.MainDbPath` points to production database path.
   - `App.LogDirectory` is valid and writable.
   - `App.BackupDirectory` is valid and writable.
   - `App.MaintenanceHistoryFileScanLimit` and `App.MaintenanceHistoryDefaultMaxRows` are appropriate for expected log volume.

## Smoke test checks
1. Start `DoctorSoft.App.exe` from publish output.
2. Validate login using production credentials.
3. Validate shell module launch:
   - Patient management
   - Accounts/ledger and accounting maintenance
   - User administration
   - Change password
   - Database utilities (backup/restore)
4. Confirm logs are written in configured log folder.

## Packaging handoff
1. Zip `release/publish` as the deployment artifact for pilot environments.
2. For formal installer packaging, use this folder as input to MSIX/WiX pipeline.
3. Attach this checklist and latest UAT sign-off to release ticket.
