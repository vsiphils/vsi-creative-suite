param(
    [string]$HewSha256 = "",
    [switch]$BuildLegacyLauncher
)

# Run this on a Windows 10/11 x64 BUILD PC with internet access and Inno Setup.
# The installer produced by this script contains all required files and runs offline.
# This script never downloads files on an end-user machine.
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$root = $PSScriptRoot
$buildId = Get-Date -Format "yyyyMMdd-HHmmss"
$work = Join-Path $root "build\$buildId"
$downloads = Join-Path $work "downloads"
$stage = Join-Path $work "stage"
$appsPath = Join-Path $stage "apps"
$licensesPath = Join-Path $stage "licenses"
$outputPath = Join-Path $root "output"
$headers = @{ "User-Agent" = "VSI-Creative-Suite-Builder"; "Accept" = "application/vnd.github+json" }

foreach ($path in @($downloads, $appsPath, $licensesPath, $outputPath, (Join-Path $stage "runtime"), (Join-Path $stage "launcher")))
{
    New-Item -Path $path -ItemType Directory -Force | Out-Null
}

function Download-File([string]$url, [string]$destination)
{
    Write-Host "Downloading $([IO.Path]::GetFileName($destination))..."
    Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -OutFile $destination
    if ((Get-Item $destination).Length -lt 1024) { throw "Download looks incomplete: $destination" }
}

function Verify-Sha256([string]$path, [string]$expected)
{
    if ($expected -notmatch '^[a-fA-F0-9]{64}$') { throw "Invalid expected SHA256 for $path" }
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($actual -ine $expected) { throw "SHA256 verification FAILED for $path" }
    Write-Host "SHA256 OK: $([IO.Path]::GetFileName($path))"
    return $actual.ToLowerInvariant()
}

function Find-Checksum([string]$checksumFile, [string]$filename)
{
    $pattern = '^(?<hash>[a-fA-F0-9]{64})\s+\*?' + [regex]::Escape($filename) + '\s*$'
    $matches = @()
    foreach ($line in Get-Content -LiteralPath $checksumFile)
    {
        $match = [regex]::Match($line.Trim(), $pattern)
        if ($match.Success) { $matches += $match.Groups["hash"].Value }
    }
    if ($matches.Count -ne 1) { throw "No unique checksum entry for $filename" }
    return $matches[0]
}

function Latest-Release([string]$repository)
{
    Write-Host "Reading release: $repository"
    return Invoke-RestMethod -Uri ("https://api.github.com/repos/" + $repository + "/releases/latest") -Headers $headers
}

function Require-Asset($release, [string]$pattern)
{
    $found = @($release.assets | Where-Object { $_.name -match $pattern })
    if ($found.Count -ne 1) { throw "Expected one release asset matching $pattern in $($release.tag_name); found $($found.Count)." }
    return $found[0]
}

function Get-ReleaseChecksum($release, [string]$archiveName)
{
    $asset = Require-Asset $release '^SHA256SUMS\.txt$'
    $file = Join-Path $downloads ("checksums-" + [guid]::NewGuid().ToString("N") + ".txt")
    Invoke-WebRequest -Uri $asset.browser_download_url -Headers $headers -UseBasicParsing -OutFile $file
    return Find-Checksum $file $archiveName
}

function Download-Licenses([string]$repo, [string]$tag, [string[]]$names)
{
    $targetDir = Join-Path $licensesPath ($repo.Replace("/", "-"))
    New-Item -Path $targetDir -ItemType Directory -Force | Out-Null
    foreach ($name in $names)
    {
        $url = "https://raw.githubusercontent.com/$repo/$tag/$name"
        $destination = Join-Path $targetDir $name
        Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -OutFile $destination
        if ((Get-Item $destination).Length -lt 50) { throw "Invalid license/notice: $repo/$name" }
    }
}

$locked = New-Object System.Collections.ArrayList
$craftApps = @("pdfcraft", "photocraft", "gridcraft", "wordcraft", "cadcraft")
foreach ($name in $craftApps)
{
    $repository = "storytold/$name"
    $release = Latest-Release $repository
    $asset = Require-Asset $release ("^" + [regex]::Escape($name) + "-.+-windows-x64-portable\.zip$")
    $zipPath = Join-Path $downloads $asset.name
    Download-File $asset.browser_download_url $zipPath
    $expected = Get-ReleaseChecksum $release $asset.name
    $actual = Verify-Sha256 $zipPath $expected
    $destination = Join-Path $appsPath $name
    New-Item -Path $destination -ItemType Directory -Force | Out-Null
    Expand-Archive -LiteralPath $zipPath -DestinationPath $destination
    $exe = @(Get-ChildItem -LiteralPath $destination -Filter "$name.exe" -File -Recurse)
    if ($exe.Count -lt 1) { throw "$name.exe was not found in the official archive." }
    Download-Licenses $repository $release.tag_name @("LICENSE-MIT", "LICENSE-APACHE", "NOTICE")
    [void]$locked.Add([pscustomobject]@{ repository=$repository; version=$release.tag_name; asset=$asset.name; sha256=$actual })
}

# Hew is a Tauri desktop application; its official Windows package is NSIS.
# It will be installed silently from this BUNDLED file, with no runtime download.
$hewRelease = Latest-Release "hew3d/hew"
$hewAsset = Require-Asset $hewRelease '^Hew_.+_x64-setup\.exe$'
$hewFile = Join-Path (Join-Path $stage "runtime") "Hew-x64-setup.exe"
Download-File $hewAsset.browser_download_url $hewFile
$hewExpected = $HewSha256
$digestProperty = $hewAsset.PSObject.Properties["digest"]
if ($hewExpected -eq "" -and $null -ne $digestProperty -and
    $null -ne $digestProperty.Value -and
    $digestProperty.Value -match '^sha256:([a-fA-F0-9]{64})$')
{
    $hewExpected = $Matches[1]
}
if ($hewExpected -eq "")
{
    # Some release formats also supply a SHA256SUMS.txt file.
    $checksumAsset = @($hewRelease.assets | Where-Object { $_.name -eq "SHA256SUMS.txt" })
    if ($checksumAsset.Count -eq 1) { $hewExpected = Get-ReleaseChecksum $hewRelease $hewAsset.name }
}
if ($hewExpected -eq "")
{
    throw "Hew's unsigned installer has no verifiable release digest. Supply a manually verified -HewSha256 value; do not bypass verification."
}
$hewActual = Verify-Sha256 $hewFile $hewExpected
Download-Licenses "hew3d/hew" $hewRelease.tag_name @("LICENSE", "LICENSE-EXCEPTION")
[void]$locked.Add([pscustomobject]@{ repository="hew3d/hew"; version=$hewRelease.tag_name; asset=$hewAsset.name; sha256=$hewActual })

# Official Evergreen STANDALONE installer, not the network bootstrapper.
$webviewFile = Join-Path (Join-Path $stage "runtime") "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"
Download-File "https://go.microsoft.com/fwlink/?linkid=2124701" $webviewFile
$signature = Get-AuthenticodeSignature -LiteralPath $webviewFile
if ($signature.Status -ne "Valid" -or $null -eq $signature.SignerCertificate -or
    $signature.SignerCertificate.Subject -notmatch "Microsoft")
{
    throw "WebView2 installer is not validly signed by Microsoft."
}
$webviewSha256 = (Get-FileHash -LiteralPath $webviewFile -Algorithm SHA256).Hash.ToLowerInvariant()
[void]$locked.Add([pscustomobject]@{ repository="Microsoft/WebView2"; version="Evergreen standalone"; asset="MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; sha256=$webviewSha256 })

Copy-Item -LiteralPath (Join-Path $root "assets\vsi-logo.png") -Destination (Join-Path $stage "launcher\vsi-logo.png")
Copy-Item -LiteralPath (Join-Path $root "THIRD_PARTY_NOTICES.txt") -Destination (Join-Path $stage "THIRD_PARTY_NOTICES.txt")
($locked | ConvertTo-Json -Depth 4) | Set-Content -LiteralPath (Join-Path $stage "release-lock.json") -Encoding UTF8

# The old, portable .NET Framework 4 compiler generates a WinForms EXE that
# can start on legacy Windows with the proper runtime installed.
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
if (!(Test-Path -LiteralPath $compiler)) { throw "Missing .NET Framework C# compiler: $compiler" }
$launcherExe = Join-Path (Join-Path $stage "launcher") "VsiCreativeSuite.exe"
$iconPath = Join-Path $root "assets\vsi-logo.ico"
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ("/out:" + $launcherExe) ("/win32icon:" + $iconPath) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $root "src\Program.cs") (Join-Path $root "src\UpdateClient.cs")
if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $launcherExe)) { throw "Launcher compilation failed." }

$innoCandidates = @(
    "$env:ProgramFiles\Inno Setup 7\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$([Environment]::GetFolderPath('ProgramFilesX86'))\Inno Setup 7\ISCC.exe",
    "$([Environment]::GetFolderPath('ProgramFilesX86'))\Inno Setup 6\ISCC.exe"
)
$iscc = $innoCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (!$iscc) { throw "Install Inno Setup 6/7 on this Windows build PC, then rerun build.ps1." }

$installerScript = Join-Path $root "packaging\VsiCreativeSuite.iss"
& $iscc "/DStageRoot=$stage" "/DProjectRoot=$root" "/O$outputPath" $installerScript
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed." }

if ($BuildLegacyLauncher)
{
    $legacyScript = Join-Path $root "packaging\VsiCreativeSuiteLegacy.iss"
    & $iscc "/DStageRoot=$stage" "/DProjectRoot=$root" "/O$outputPath" $legacyScript
    if ($LASTEXITCODE -ne 0) { throw "Legacy launcher package build failed." }
}

Write-Host "Build finished. Offline installers are in: $outputPath"
Write-Host "IMPORTANT: Test installation, Hew detection, each app, and air-gapped launch on Windows 10 AND Windows 11 before deployment."
