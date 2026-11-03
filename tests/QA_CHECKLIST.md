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
- [ ] Launcher has no network calls, login or telemetry.
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

**Do not claim the full suite is production-ready until all applicable checks pass.**
