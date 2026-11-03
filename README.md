# VSI Creative Suite — Offline Windows Launcher

Company-branded launcher for **Vertical Space Interiors, Inc.** with six initial open-source tools:

| Tile | Official project | How it is bundled |
| --- | --- | --- |
| Hew 3D | https://github.com/hew3d/hew | Official Windows x64 NSIS installer, executed offline |
| PdfCraft | https://github.com/storytold/pdfcraft | Official Windows x64 portable ZIP |
| PhotoCraft | https://github.com/storytold/photocraft | Official Windows x64 portable ZIP |
| GridCraft | https://github.com/storytold/gridcraft | Official Windows x64 portable ZIP |
| WordCraft | https://github.com/storytold/wordcraft | Official Windows x64 portable ZIP |
| CADCraft | https://github.com/storytold/cadcraft | Official Windows x64 portable ZIP |

The custom launcher has no accounts, telemetry, embedded browser, or third-party ads. On Windows 10/11, v0.3.0 makes a background check of the official public GitHub Releases API when opened, and the footer has a manual **CHECK FOR UPDATES** button. The launcher remains usable offline. The **COMPANY EDITION** button opens `https://vsiphils.com/` in the default browser only when clicked. Each **OPEN** button starts a local executable. If an app is missing, it says **MISSING**.

## Status

**Version 0.3.0 updater candidate (not yet a published installer).** This branch adds an update prompt, full-bundle download with GitHub asset SHA-256 verification, and a Windows compile/test workflow. The installed v0.2.1 launcher does not have an updater; v0.3.0 must be installed once manually. Existing offline launcher/card behavior should remain intact but still needs Windows 10/11 acceptance testing.

## Requirements to create the one-file offline installer

- An **online Windows 10/11 x64 build PC** (only the builder needs internet)
- Windows PowerShell 5.1
- .NET Framework 4.x with the standard C# compiler at
  `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`
- [Inno Setup 6 or 7](https://jrsoftware.org/isinfo.php)
- Sufficient temporary disk space for all six downloaded applications plus installer compression

The build automatically:

1. Downloads the latest official Windows x64 portable archives from the **five** Craft repositories.
2. Verifies all five archives against each upstream `SHA256SUMS.txt`.
3. Downloads the latest official Hew x64 NSIS installer, verifies its GitHub release SHA-256 digest (or requires a manually verified `-HewSha256`).
4. Includes the official **Microsoft WebView2 Evergreen Standalone x64** installer for first-time offline use; verifies its Microsoft signature.
5. Downloads license texts, stages all applications, and compiles the custom VSI-branded launcher.
6. Compiles `VSI-Creative-Suite-Setup-Win10-11-x64.exe` with Inno Setup.
7. Saves a `release-lock.json` in the installed app so exact packaged versions and checksums can be audited.

No internet download occurs during an end-user installation.

### One-line Windows build command

Open PowerShell in this project folder and run:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1`

If Hew's release has no verifiable `digest` or `SHA256SUMS.txt`, the build will stop. Obtain the **actual** SHA-256 through a separate trusted verification procedure, then run:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -HewSha256 ACTUAL_VERIFIED_64_CHARACTER_HASH`

Never invent a checksum or bypass that stop condition. If your installation policy requires a fixed approved version, keep the generated `release-lock.json` with each published installer; this initial builder uses each project's *latest* release at build time.

The installer appears in `output\`. The client machine does **not** run `build.ps1` and does **not** need an internet connection.

### Rebuild only the launcher/installer (no re-download)

If this Windows build PC already has a complete `build\<date>\stage` folder from a successful build, you can update the design and packaging without downloading the large app files again. Extract the updated source over the project files **without removing the existing `build` folder**, then run:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\repack.ps1`

This recompiles `src\Program.cs` and `src\UpdateClient.cs` into the last downloaded stage and rebuilds the one-file offline installer. **Important: repack.ps1 does not download new versions of the six apps.** For a release containing all newest upstream apps, use `build.ps1` on an internet-connected build machine.


## VSI Creative Suite v0.3.0 updates (Windows 10/11 only)

**Update prompt:** on startup (or when **CHECK FOR UPDATES** is clicked) the launcher queries the public GitHub Releases API in the background. If a newer stable suite release is found, it shows `VSI Creative Suite vX.Y.Z is available. Do you want to download and install the update?` with **Yes / No**. Choosing No never changes the installation. Choosing Yes downloads the **full offline suite installer** with progress, checks the exact asset size and SHA-256 digest against GitHub's release API, then starts the interactive installer. The user can cancel a download and the installed suite remains unchanged. GitHub HTTP 404 (no published stable release yet) is treated as **no update**, not a failure dialog. Other network errors do not block opening installed apps.

**All bundled app updates:** the full-suite installer includes the exact versions captured in `release-lock.json`. To update **Hew + the five Craft apps** at the same time, always run `build.ps1` before publishing a new release; this builder fetches current upstream releases. A simple Git commit does not update installed users. The installed app does not separately reach upstream repositories. A newly added seventh app needs to be added to the launcher and the full build/packaging first; it will then arrive through the next suite release.

**First-time migration:** v0.2.1 cannot self-update. Each PC must install updater-enabled v0.3.0 once; subsequent newer stable GitHub Releases can be detected automatically.

**Release publishing checklist:**

1. Update the three version values together: `SuiteUpdater.CurrentVersion` in `src/UpdateClient.cs`, `AppVersion` and `AppVerName` in `packaging/VsiCreativeSuite.iss` (plus the optional legacy installer). Increment to `0.3.1` or newer for the next release.
2. Run the full one-line `build.ps1` command on the Windows build PC, **not repack.ps1**. Inspect `release-lock.json` to confirm which Hew/Craft versions are included.
3. Test installation, app open/save, offline behavior and upgrade from the prior version on Windows 10 and 11 x64. Test cancellation and SHA mismatch handling.
4. Publish a stable (not draft/prerelease) GitHub Release with tag `v0.3.0` and upload the resulting exact asset named `VSI-Creative-Suite-Setup-Win10-11-x64.exe`. Enable GitHub immutable releases where available.
5. Confirm GitHub reports a `sha256:<64 hex>` asset digest. If absent, the updater refuses to install, as designed. Also check installer reputation/signing and anti-virus results before production.

**Security:** the v0.3.0 prototype checks HTTPS, a fixed official GitHub asset URL, file length, SHA-256 and version. This detects corruption and mismatched downloads but **is not independent publisher authentication**: a compromised GitHub repository could replace metadata and binary together. For real company-wide rollout, sign the combined installer using VSI's trusted code-signing certificate and enforce the expected signer (or a separately signed release manifest) in the updater. Windows SmartScreen prompts are expected for unsigned binaries. Do not treat SHA-256 alone as equivalent to code signing or bypass Windows security dialogs.

## How installation works on Windows 10/11

- The installer copies the portable PdfCraft, PhotoCraft, GridCraft, WordCraft, and CADCraft apps into its own installation directory.
- It installs WebView2 from an **embedded standalone installer**, only if missing.
- It silently installs the bundled official Hew NSIS package. Hew remains an independent native application.
- After installing Hew, it removes the newly created extra `Hew.lnk` desktop shortcut, if the upstream installer created one. Existing shortcuts are preserved. For a desktop shortcut left by an **older** VSI Suite install, remove that shortcut manually if it is unwanted.
- The custom launcher finds Hew through common native install paths and Windows uninstall registry metadata.
- The user gets a Start menu shortcut for VSI Creative Suite; the desktop shortcut is optional.
- Uninstalling VSI Creative Suite removes the launcher and five bundled portable apps, **but intentionally does not uninstall Hew or the shared WebView2 runtime**, which might also be used elsewhere.

### Offline acceptance test before rollout

1. On a clean Windows 10 x64 VM, **disconnect the internet before running** the final Setup EXE.
2. Install. Verify there are exactly six cards and all six show Installed.
3. Click each OPEN button and verify it starts the actual application.
4. Create and save a sample file in every app, close it, reopen it without internet.
5. Repeat on a clean Windows 11 x64 VM.
6. Verify uninstall behavior, file associations, antivirus/SmartScreen behavior, shortcut, install size, and app data paths.
7. **Do not deploy to production PCs unless these tests pass.** This repo contains the acceptance checklist in `tests/QA_CHECKLIST.md`.

Windows SmartScreen may warn about the **custom VSI launcher and installer** until VSI signs them with a trusted code-signing certificate. Some upstream installers are signed, but signing does not automatically transfer to our combined installer.

## Windows 7/8/8.1 — optional legacy launcher

The native-looking launcher is written against .NET Framework 4.x APIs, so it can **potentially** run on Windows 7 SP1 and Windows 8/8.1 with a compatible .NET runtime. The upstream versions of these modern creative apps are **not supported/verified** on legacy Windows. The launcher deliberately shows `Windows 10+ required` and disables their buttons on older systems.

To additionally compile the *experimental launcher-only* installer:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -BuildLegacyLauncher`

This produces `VSI-Creative-Suite-Legacy-Launcher-Win7-8.exe` (UI only; no apps); its useful purpose is testing corporate branding and installer compatibility, **not an offline creative suite for Windows 7**. Test on a fully isolated legacy VM. Windows 7 is past Microsoft's security support period, so it must not be treated as a secure production platform.

## Project layout

- `src/Program.cs` — company-branded offline Windows Forms launcher
- `src/UpdateClient.cs` — GitHub Releases updater, prompt, progress and SHA-256 verification
- `tests/UpdaterTests.cs` — offline unit tests for update metadata and SHA-256 checks
- `assets/vsi-logo.png` — original uploaded company logo, unchanged
- `assets/vsi-logo.ico` — Windows icon generated from the uploaded logo
- `build.ps1` — download, verify, stage, compile, bundle (Windows build PC only)
- `repack.ps1` — recompile launcher and installer from an existing staged bundle (no downloads)
- `packaging/VsiCreativeSuite.iss` — full offline Windows 10/11 installer
- `packaging/VsiCreativeSuiteLegacy.iss` — experimental legacy UI installer
- `THIRD_PARTY_NOTICES.txt` — source and license attribution
- `tests/QA_CHECKLIST.md` — release acceptance tests

## Ownership, license and update expectations

VSI owns its **custom launcher branding and deployment configuration**, not the Hew or Craft applications. All upstream licenses and notices must remain included when distributing internally or externally; Hew's AGPL requirements require particular care if it is modified or distributed. The code does not alter or rebrand the third-party binaries. Versions are pinned in a build's `release-lock.json`, and the updater downloads a newly published and approved full offline installer. Building and QA of each new suite installer are still required. The six tools are actively developed and several remain alpha-quality, so validate real company drawings, PSDs, PDFs, XLSX and DOCX before replacing paid tools.

The launcher UI no longer shows a "Licenses & Credits" button; the upstream notices and attribution files are still packaged under the installation folder for legal compliance. Inno Setup 7 may require a commercial license for company use; verify this before broad deployment.

## Troubleshooting

- **MISSING app**: Check that its staged executable exists under the installed `apps\<name>` directory; for Hew, verify the separate upstream install finished and the EXE is discoverable in the registry.
- **App won't open**: Open the original app directly to get its error. GPU driver and DirectX availability may matter.
- **Setup downloaded something during install**: This is a release blocker; use only the standalone WebView2 binary and embedded Hew installer.
- **Windows 7/8 cards disabled**: Expected. Full suite support is Windows 10/11 x64 only.
- **Hew verification stops build**: Obtain and pin a trusted exact SHA-256 for that release, or wait for an official release digest. Never skip integrity checking.
