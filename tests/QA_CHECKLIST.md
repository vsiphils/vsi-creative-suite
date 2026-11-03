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
- [ ] Launcher makes only its optional official GitHub Releases update check (no login or telemetry); offline mode remains fully usable.
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

## Updater regression tests (v0.3.0+)

- [ ] CI compiles both source files and passes updater metadata and SHA-256 unit tests.
- [ ] Fresh v0.3.0 install has a visible CHECK FOR UPDATES button; manual click reports up-to-date when no newer stable release exists.
- [ ] Startup GitHub API error, rate-limit, DNS failure and disconnected network do not interrupt using offline apps.
- [ ] On a test newer stable release with the exact full offline installer asset and SHA256 digest, the launcher prompts Yes/No before downloading.
- [ ] No leaves installed files untouched; Cancel aborts a partial download without running anything.
- [ ] Yes downloads full installer with progress; size or checksum mismatch stops the installation.
- [ ] Upgrade from v0.3.0 to a real newer test release preserves user documents and starts the interactive installer without silently closing running apps.
- [ ] A bundled app updated in the full suite's release-lock.json actually reports/launches the new upstream version after install.
- [ ] A newly packaged seventh app appears only after new launcher and full installer have been published.
- [ ] Old release, draft/prerelease, unexpected download URL, missing digest, and missing installer asset are rejected.
- [ ] Verify publisher identity with a trusted code-signing certificate or separately signed manifest before company rollout; SHA-256 alone is not enough.

**Do not claim the full suite is production-ready until all applicable checks pass.**
