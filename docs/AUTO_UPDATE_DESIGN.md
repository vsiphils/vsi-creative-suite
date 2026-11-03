# VSI Creative Suite v0.3.0 update architecture (source candidate)

Status: v0.3.0 runtime updater has been added to source but not tested as a complete offline installer or deployed. Existing v0.2.1 installations cannot self-update. SHA-256 verification is implemented; publisher code-signing verification is still required for production.

## User experience goal

- When online, the launcher periodically checks a trusted, versioned GitHub Release published from vsiphils/vsi-creative-suite.
- When a newer stable version exists, show a Yes/No prompt before downloading the full offline-capable installer; download with progress, verify size and SHA-256 digest, then launch the interactive installer. Never silently close an app containing unsaved user documents.
- When offline, all already installed apps remain fully functional. No GitHub sign-in or network dependency for startup.
- Newly introduced apps appear only after their installer/binaries pass validation and the suite update is installed; adding a Git commit or catalog entry alone will not install arbitrary code.

## Intended release workflow

1. Developer updates version and app catalog on a feature branch and provides vetted x64 payloads plus upstream attribution/licensing.
2. Windows CI validates the catalog and compiles/tests the C# updater. It does not yet create, sign, scan, or publish the full suite installer.
3. Maintainer publishes a signed, immutable stable GitHub Release after explicit manual QA. The Release includes full offline installer and a signed update manifest.
4. Installed launchers only trust the pinned publisher signing certificate/public key; HTTPS and checksums are additional integrity measures, not substitutes for a trusted signer.
5. Updater downloads to a per-user local update directory after consent and verifies size and SHA-256. It then launches the interactive installer and exits. Independent signing verification and installer rollback are still TODO.
6. A new app requires an updated launcher definition, bundled executable and a full suite release. Local dynamic catalog rendering remains TODO.

## Security and operability requirements

- Do not run a PowerShell command fetched from the internet or execute unsigned code solely because a GitHub branch changed.
- Do not treat GitHub public write access as evidence of code safety. Avoid floating assets, untrusted redirects, path traversal in ZIPs and downgrade/replay attacks.
- No personal access tokens or GitHub secrets embedded in shipped binaries; public GitHub Releases require no auth.
- Rate limit update checks, use timeout and exponential backoff. App works with blocked GitHub/network.
- Installers should keep bundled app notices/licenses and provide paths to relevant upstream projects even when the launcher UI does not show a Credits button.
- Test fresh Windows 10 and 11 x64 installs, upgrade from 0.2.1, standard-user/UAC, cancellation, rollback, forced offline, and newly added app payload end-to-end.

## Implemented in this PR

- catalog/apps.json contains six installed applications; remote package fields are null until approved release artifacts exist.
- scripts/validate-catalog.ps1 validates catalog schema, identifiers, versions, executable names, delivery types and package hash format.
- Windows GitHub Actions checks the catalog on PRs and selected changes to main. It does not publish installers.
- Runtime GitHub Releases check, Yes/No update prompt, download progress, size/SHA-256 checks, and starting the interactive full installer are implemented in src/UpdateClient.cs. Windows C# compile and updater metadata tests run on PRs.
- Pending before production: genuine VSI code signing and signer verification, real upgrade/offline QA on Windows 10/11, rollback/recovery tests, scheduled upstream release monitoring, and dynamic app tiles for separately packaged apps.
