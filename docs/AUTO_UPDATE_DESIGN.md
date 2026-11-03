# VSI Creative Suite update architecture (proposed, not shipped)

Status: Design and validation foundation only. Version 0.2.1 does not check for or install updates. This branch adds no runtime update behavior.

## User experience goal

- When online, the launcher periodically checks a trusted, versioned GitHub Release published from vsiphils/vsi-creative-suite.
- When a verified newer stable version exists, download its complete offline-capable installer in the background, then request permission to install at a suitable time. Never replace files used by a running app.
- When offline, all already installed apps remain fully functional. No GitHub sign-in or network dependency for startup.
- Newly introduced apps appear only after their installer/binaries pass validation and the suite update is installed; adding a Git commit or catalog entry alone will not install arbitrary code.

## Intended release workflow

1. Developer updates version and app catalog on a feature branch and provides vetted x64 payloads plus upstream attribution/licensing.
2. Windows CI validates the catalog, builds MSI/EXE installer from an isolated environment, scans/tests it and records SHA256.
3. Maintainer publishes a signed, immutable stable GitHub Release after explicit manual QA. The Release includes full offline installer and a signed update manifest.
4. Installed launchers only trust the pinned publisher signing certificate/public key; HTTPS and checksums are additional integrity measures, not substitutes for a trusted signer.
5. Updater downloads to per-user temp location, verifies signature/hash and correct architecture/version, asks consent, closes/restarts launcher, then installs. Failures leave the previous working version intact.
6. Program registers the newly installed app from its local manifest. Removed apps do not silently erase user documents or app data.

## Security and operability requirements

- Do not run a PowerShell command fetched from the internet or execute unsigned code solely because a GitHub branch changed.
- Do not treat GitHub public write access as evidence of code safety. Avoid floating assets, untrusted redirects, path traversal in ZIPs and downgrade/replay attacks.
- No personal access tokens or GitHub secrets embedded in shipped binaries; public GitHub Releases require no auth.
- Rate limit update checks, use timeout and exponential backoff. App works with blocked GitHub/network.
- Installers should keep bundled app notices/licenses and provide paths to relevant upstream projects even when the launcher UI does not show a Credits button.
- Test fresh Windows 10 and 11 x64 installs, upgrade from 0.2.1, standard-user/UAC, cancellation, rollback, forced offline, and newly added app payload end-to-end.

## Current implementation in this PR

- catalog/apps.json contains six installed applications; remote package fields are null until approved release artifacts exist.
- scripts/validate-catalog.ps1 validates catalog schema, identifiers, versions, executable names, delivery types and package hash format.
- Windows GitHub Actions checks the catalog on PRs and selected changes to main. It does not publish installers.
- Runtime catalog parsing, update checks, signature verification, safe installer orchestration, rollback, and installation are still TODO.
