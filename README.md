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
| DeckCraft (optional) | https://github.com/storytold/deckcraft | Official Windows x64 portable ZIP, only if user chooses **INSTALL** |

The custom launcher has no accounts, telemetry, embedded browser, or third-party ads. On Windows 10/11, v0.3.1 checks for optional launcher and individual app updates from official public GitHub releases when opened, and the footer has a manual **CHECK FOR UPDATES** button. The launcher remains usable offline. The **COMPANY EDITION** button opens `https://vsiphils.com/` in the default browser only when clicked. Each **OPEN** button starts a local executable. When an approved app is added to the GitHub catalog, an **INSTALL** tile can appear; clicking it offers an optional installation.

## Status

**Version 0.3.1 selective-updater candidate (not yet a published installer).** Feature branch under review. GitHub Action compilation/tests do not replace Windows installation, launch, update and recovery acceptance testing. Existing v0.3.0 installations need this release installed once manually to gain per-app update selection.

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

This recompiles `src\Program.cs`, `src\UpdateClient.cs` and `src\AppUpdateClient.cs` into the last downloaded stage and rebuilds the one-file offline installer. **Important: repack.ps1 does not download new versions of the six apps.** For a release containing all newest upstream apps, use `build.ps1` on an internet-connected build machine.


## Optional updates — VSI Creative Suite v0.3.1 candidate (Windows 10/11)

**Launcher updates:** at startup and via **CHECK FOR UPDATES**, VSI checks the public stable **VSI Creative Suite GitHub Release**. If a newer launcher/full-suite release exists, the user chooses whether to download the official full offline installer. Declining the full installer continues on to the per-app update scan; nothing installs without consent. The full installer is a bundle and may also replace bundled apps: for app-by-app control, use the selective updater below instead. A new Git commit without a published Release is **not** a launcher upgrade. The initial launcher version must be manually installed once to enable these features.

**Independent app updates:** the same check also reads the official stable GitHub Release of each approved app (Hew, PdfCraft, PhotoCraft, GridCraft, WordCraft, CADCraft). This solves the case where the VSI launcher says no newer **suite** release but WordCraft itself offers v0.15.1. A checkbox dialog lists each verified newer version. Nothing is preselected: choose one, several, **Select all**, **Clear**, **Install / Update**, or **Later**. Only the checked apps are downloaded and updated. **Close selected apps and save documents first.** Downloads are checked for expected size and the GitHub release SHA-256 digest; bad metadata or archives are rejected. Portable ZIP applications are extracted into a separate staging folder before replacement. Old per-app directories are retained as recovery backups. Hew uses its upstream installer; the updater removes only a **new** Hew desktop shortcut, preserving pre-existing shortcuts. Errors are shown without blocking other already installed apps from launching offline.

**DeckCraft test entry:** the reviewed app catalog includes DeckCraft (Presentations and Slides) as an optional seventh app. Its stable Windows x64 portable release is `v0.15.0`; it is not bundled or silently installed. The new tile should appear after the v0.3.1 launcher successfully checks the approved catalog. On main before this test, the catalog did not exist; publishing only the approved metadata makes the feature testable without shipping a new executable.

**New app catalog:** the launcher reads the reviewed public catalog from [catalog/apps.json](catalog/apps.json) on `main`. New approved apps automatically gain an **INSTALL** tile when checked online; they are *not downloaded or installed automatically*. When the user checks that new app, the updater obtains the official x64 portable ZIP from the specified public GitHub repo, verifies SHA-256, and installs only with user approval. A validated catalog is cached for offline display. An offline launcher with no catalog access uses its bundled snapshot. New apps are currently limited to Windows x64 **portable ZIP** releases; new native installers other than the already reviewed Hew package are not supported for security.

To add an app, edit `catalog/apps.json` in a reviewed PR on main. Example for a hypothetical approved portable app (the repo and asset must **actually exist** before publishing):

```json
{
  "id": "newcraft",
  "name": "NewCraft",
  "description": "Optional creative tool",
  "executable": "newcraft.exe",
  "symbol": "N",
  "accent": "#3377DD",
  "delivery": "release-zip",
  "repository": "TRUSTED-OWNER/newcraft",
  "assetNameTemplate": "newcraft-{version}-windows-x64-portable.zip"
}
```

This object is added inside the `apps` array. Keep `schemaVersion: 1`, and require a stable upstream release tagged `vMAJOR.MINOR.PATCH` with a matching asset and GitHub SHA-256 digest. Do **not** add an untrusted publisher; catalog entries authorize downloading and executing code on VSI PCs. Changes on a feature branch are not visible in installed clients until merged to `main`.

**Release publishing checklist:**

1. Keep `SuiteUpdater.CurrentVersion`, Inno `AppVersion/AppVerName`, and `catalog.suiteVersion` aligned. Current candidate is `0.3.1`.
2. Run `build.ps1` on a Windows 10/11 build PC to package latest upstream versions or `repack.ps1` to reuse existing fully staged assets and rebuild just the launcher/installer.
3. Validate the checkboxes, update one app while leaving others unchanged, cancel an update, restore from backups, recognize new catalog apps, and launch all apps on Windows 10 and 11. Test disconnected mode, DPI scaling and standard-user permissions.
4. Publish a **tested** stable GitHub Release tagged `v0.3.1` with asset `VSI-Creative-Suite-Setup-Win10-11-x64.exe` and its GitHub `sha256:<64 hex>` digest. Until then the updater may correctly report no newer suite release.
5. Sign executables and validate the publisher's code-signing identity or a separately signed manifest before a company-wide rollout. SHA-256 from GitHub identifies downloaded bytes but is not independent publisher authentication.

**Network and update behavior:** the installed creative apps still run offline. Updates need internet. Unauthenticated GitHub Releases API checks are subject to GitHub rate limits, especially on a shared office WAN IP; a future company-scale deployment should distribute a trusted cached update feed rather than having many desktops query every repository individually. The updater must never bypass TLS, SmartScreen or antivirus warnings. A user may decline all updates and continue using existing apps.

## Windows release candidate build (GitHub Actions)

The dedicated [package candidate workflow](.github/workflows/package-rc.yml) builds the **full offline installer** on a Windows runner from the official upstream packages, verifies recorded SHA-256 values, checks the reviewed seven-app catalog (including optional DeckCraft), and uploads an **unsigned, untested release candidate artifact**. It does not merge this PR or create a public stable GitHub Release. Download the candidate from the successful Actions run, manually test on an isolated Windows 10/11 workstation, and verify the installer and optional app updates before publishing. The artifact is not a substitute for code-signing or Windows acceptance.

**Inno Setup commercial licensing:** the official [publisher's FAQ](https://jrsoftware.org/isorder.php) classifies company internal builds as commercial use and requests a license even for in-house installers, but states purchase is **not strictly required**. Its open software license permits commercial use. Respect the publisher's terms and consider purchasing before production distribution.

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
