# Release acceptance checklist

## Supply-chain verification

- [ ] Record exact upstream tag, asset, SHA-256 in release-lock.json for all six apps.
- [ ] Five portable Craft ZIPs match upstream SHA256SUMS.txt.
- [ ] Hew NSIS asset matches its official GitHub release digest or independently verified hash.
- [ ] WebView2 offline installer has a valid Microsoft Authenticode signature.
- [ ] NOTICE and upstream license texts are in the installed licenses folder.
- [ ] No third-party company trademark/logo is represented as a VSI-owned mark.

## Offline Windows 10 x64 (clean VM, disconnect network before install)

- [ ] One setup EXE installs fully without connecting to GitHub or Microsoft.
- [ ] Launcher starts and displays original VSI logo, correct name, exactly six cards.
- [ ] PdfCraft OPEN launches and saves/reopens a local PDF.
- [ ] PhotoCraft OPEN launches and saves/reopens a local image.
- [ ] GridCraft OPEN launches and saves/reopens a local workbook.
- [ ] WordCraft OPEN launches and saves/reopens a local document.
- [ ] CADCraft OPEN launches and saves/reopens a local drawing.
- [ ] Hew 3D OPEN launches and saves/reopens a local model.
- [ ] Missing/corrupt binary disables its card instead of falsely showing Installed.
- [ ] WebView2 installs offline if not preexisting; preexisting runtime is preserved.
- [ ] Launcher optionally checks only approved GitHub releases for the suite and installed/catalog apps (no login or telemetry); offline mode remains fully usable.
- [ ] Start menu works, desktop shortcut appears only if selected.
- [ ] Fresh install does not leave a second desktop shortcut called `Hew` (Hew remains installed and launchable inside VSI).
- [ ] A pre-existing user-owned `Hew.lnk` shortcut remains untouched by installation.
- [ ] Footer shows `There's issue? Contact VSI I.T`, not `LICENSES & CREDITS`.
- [ ] `COMPANY EDITION` opens `https://vsiphils.com/` only after explicit click; launcher does not open it on startup.
- [ ] Rounded card layout remains readable at minimum window size and 150% display scaling.
- [ ] Card icons and orange OPEN actions have NO opaque black square behind their rounded corners (v0.2.0 regression).
- [ ] Company Edition footer link and header offline badge have rounded surfaces with no black rectangular backing.
- [ ] Mouse hover and click on each painted OPEN button launches the correct app; keyboard Enter/Space while a card is focused works.
- [ ] Uninstall removes launcher and portable apps but leaves Hew/WebView2 in place.

## Offline Windows 11 x64 (clean VM)

- [ ] Repeat every Windows 10 test.
- [ ] Check GPU-driver behavior, DPI scaling at 100% and 150%, and small display layout.
- [ ] Inspect antivirus/SmartScreen prompts and publisher/signature status.

## Legacy launcher Windows 7 SP1 / Windows 8 / 8.1 (isolated VMs)

- [ ] Confirm compatible .NET Framework 4.x runtime is installed.
- [ ] Legacy launcher starts.
- [ ] All six cards explicitly report Windows 10+ required and cannot launch.
- [ ] No app bundles/unsupported executables are installed on legacy Windows.

## Updater regression tests (v0.3.1 candidate)

- [ ] Windows CI compiles the three C# source files and passes suite, upstream app, catalog and ZIP safety unit tests.
- [ ] Fresh install has **CHECK FOR UPDATES** and bundled six cards; detects a catalog-approved seventh tile online without silently installing anything.
- [ ] If VSI has no published stable Release, the check still queries **individual app** releases (e.g. WordCraft v0.15.1 vs locally bundled v0.15.0).
- [ ] A verified newer upstream app release appears with a checkbox **unchecked by default**; no downloads/installations occur before selection.
- [ ] **Later** leaves every app and launcher untouched; **Clear** unchecks all; **Select all** checks all; **Install / Update** updates only checked apps.
- [ ] Select only WordCraft: new WordCraft opens and reports latest version, while PdfCraft, PhotoCraft, GridCraft, CADCraft and Hew versions are unchanged.
- [ ] Select two apps: both update; others remain unchanged. Updating a closed portable app does not erase documents stored elsewhere.
- [ ] Run with a selected app still open: show a warning and do not replace its folder.
- [ ] Update downloads validate exact GitHub-owned upstream release URL, reported SHA-256 digest, size and ZIP path safety. Reject malicious ZIP entry, invalid hash, unapproved owner, or bad release tag.
- [ ] Cancel during download: no app files changed. Failure during installation restores the affected app directory; recovery backup is retained.
- [ ] Hew installer update runs only after consent; new Hew desktop shortcut removed; a preexisting desktop shortcut preserved.
- [ ] A new catalog app added in `main` shows **INSTALL** and an optional **NEW APP** checkbox after an online scan; unselected app stays absent.
- [ ] After accepting install, the new approved app appears **Installed** and can be opened; works offline on subsequent launches.
- [ ] Offline catalog fallback displays last approved cached entries, but never attempts an install without a verified download.
- [ ] Suite full installer offer can be declined and still reach app-specific selection. Accepting the full installer displays the fact that it may replace all bundled apps.
- [ ] A GitHub repository with no stable VSI suite Release is not misreported as a networking failure; network 403/500, DNS and rate limiting do not prevent offline app use.
- [ ] Update check does not repeatedly offer a version just updated through the VSI updater (release-lock is maintained).
- [ ] Test offline Windows 10/11, 100/150% DPI, standard user, low free disk space and antivirus interference.
- [ ] Verify publisher identity with a trusted code-signing certificate or separately signed manifest before company rollout; SHA-256 alone is not enough.

**Do not deploy company-wide until every applicable Windows acceptance test passes.**
