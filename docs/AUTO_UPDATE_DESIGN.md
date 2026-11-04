# VSI Creative Suite v0.3.1 — optional launcher and app updates (test candidate)

Status: source changes are on the draft PR. Windows GitHub Actions compiles the launcher and updater tests; **real Windows installer, update, rollback and offline acceptance tests are not yet complete**. Do not roll out company-wide.

## What users see

- On startup and when **CHECK FOR UPDATES** is clicked, the launcher checks the official VSI public GitHub stable Releases for a **full suite/launcher** update.
- If a newer full suite installer exists, users choose Yes/No. Yes downloads and verifies the complete installer; No continues with a separate **individual app** scan. A full suite installer may also replace the apps bundled into it.
- The app scan reads the six approved upstream official GitHub stable releases; even if VSI itself has no new Release, individual WordCraft/PdfCraft/etc. updates can be offered.
- A checkable list shows only confirmed newer app releases plus optional approved **NEW APP** entries. **Nothing is selected by default.** The user can **Select all**, **Clear**, **Install / Update**, or **Later**.
- Only selected apps download. All apps remain locally runnable offline, and updates never run silently without confirmation.
- If maintainers add an approved app in `main/catalog/apps.json`, a newly installed v0.3.1 launcher can show that app dynamically with an **INSTALL** action, without first rebuilding the launcher. This **does not automatically install** the new app. The new app requires an official semver stable release and matching Windows x64 portable ZIP with SHA-256 digest.
- An approved catalog snapshot is cached for offline tile display.

## Sources and trust

- Full suite/launcher release API: `https://api.github.com/repos/vsiphils/vsi-creative-suite/releases/latest`.
- Reviewed app catalog: `https://raw.githubusercontent.com/vsiphils/vsi-creative-suite/main/catalog/apps.json`.
- Per-app latest stable release: `https://api.github.com/repos/<APPROVED-OWNER>/<APPROVED-REPO>/releases/latest`.
- Upstream asset URL must be exactly a GitHub `/releases/download/vX.Y.Z/<expected-asset>`; reported size and `sha256:` digest are mandatory. Downloaded bytes must match both.
- ZIPs are extracted into a fresh app staging directory; reject traversal, absolute names, symlinks, too many entries and a cumulative uncompressed content ceiling before replacing an app.
- Running apps are rejected for replacement. Previously installed portable-app directories are retained in `.vsi-backup-...` for recovery. On failure during an individual app replacement, the current affected app's previous folder is restored where possible; earlier successfully updated apps remain updated. Hew's native installer is independent and is not automatically rolled back.
- No tokens are embedded in the installer. GitHub public API may rate-limit many PCs sharing the same office public IP.
- GitHub SHA-256 verification detects package mismatch but is **not independent publisher identity verification**. Trusted publisher code signing or a separately signed update manifest, real offline/Win10/Win11 acceptance, recovery, endpoint AV/SmartScreen, and Inno commercial licensing review remain release blockers.

## Maintainer workflow

1. Add or edit approved app metadata in `catalog/apps.json` on a reviewed pull request. New apps use `delivery: "release-zip"`, `repository`, `assetNameTemplate` and `executable`. Do not add untrusted publishers. Only approved `main` catalog changes reach installed clients.
2. Run `scripts/validate-catalog.ps1`, then Windows CI compilation and unit tests.
3. Rebuild full installer with `build.ps1` (fresh upstream bundle) or `repack.ps1` (last verified staged bundle, launcher-only source compilation). Confirm the package version matches `SuiteUpdater.CurrentVersion`, Inno version and catalog suite version.
4. Manually test on clean Windows 10/11 (select one/two/none, download cancel, interrupted ZIP, Hew shortcut preservation, new app install, offline fallback, rollback, no administrative permissions). CI passing does **not** mean this has been done.
5. Publish a vetted and signed stable VSI Release only after checks. A plain Git commit does not produce a full suite updater notification; catalog additions appear separately only if an official upstream release exists.

## Known limitations / next iteration

- Per-app latest checks query the unauthenticated GitHub API separately on each connected PC; aggregate office use may hit shared-IP rate limits. Implement a cached, independently signed manifest feed for large deployments.
- The app version lock is updated when **VSI** updates an app. Apps that self-update independently can leave their VSI version lock behind until reconciled.
- Rollback is per affected portable app, not a global atomic multi-app transaction; backups are preserved, not garbage-collected.
- New app support is intentionally limited to approved official x64 portable ZIPs, not arbitrary scripts or installers.
