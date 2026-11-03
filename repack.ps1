# Recompile updated VSI launcher and repack the last fully downloaded stage.
# Run on the same Windows 10/11 x64 build PC. No internet/downloads required.
param([switch]$BuildLegacyLauncher)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$buildDirectory = Join-Path $root 'build'
if (!(Test-Path -LiteralPath $buildDirectory)) { throw 'No existing build folder. Run build.ps1 first.' }
$candidate = Get-ChildItem -LiteralPath $buildDirectory -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'stage\apps') } | Sort-Object Name -Descending | Select-Object -First 1
if ($null -eq $candidate) { throw 'No existing fully staged app bundle. Run build.ps1 first.' }
$stage = Join-Path $candidate.FullName 'stage'
$launcherDirectory = Join-Path $stage 'launcher'
$launcherExe = Join-Path $launcherDirectory 'VsiCreativeSuite.exe'

foreach ($path in @('apps', 'runtime\Hew-x64-setup.exe', 'runtime\MicrosoftEdgeWebView2RuntimeInstallerX64.exe', 'release-lock.json'))
{
    if (!(Test-Path -LiteralPath (Join-Path $stage $path))) { throw "Missing staged content: $path. Run build.ps1 again." }
}
Copy-Item -LiteralPath (Join-Path $root 'assets\vsi-logo.png') -Destination (Join-Path $launcherDirectory 'vsi-logo.png') -Force
Copy-Item -LiteralPath (Join-Path $root 'THIRD_PARTY_NOTICES.txt') -Destination (Join-Path $stage 'THIRD_PARTY_NOTICES.txt') -Force

$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Missing .NET Framework 4 C# compiler.' }
$icon = Join-Path $root 'assets\vsi-logo.ico'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ("/out:" + $launcherExe) ("/win32icon:" + $icon) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll (Join-Path $root 'src\Program.cs')
if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $launcherExe)) { throw 'Launcher compilation failed. No installer was built.' }

$innoCandidates = @("$env:ProgramFiles\Inno Setup 7\ISCC.exe", "$([Environment]::GetFolderPath('ProgramFilesX86'))\Inno Setup 7\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe", "$([Environment]::GetFolderPath('ProgramFilesX86'))\Inno Setup 6\ISCC.exe")
$iscc = $innoCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (!$iscc) { throw 'Inno Setup compiler ISCC.exe not found.' }
$out = Join-Path $root 'output'
New-Item -Path $out -ItemType Directory -Force | Out-Null
& $iscc "/DStageRoot=$stage" "/DProjectRoot=$root" "/O$out" (Join-Path $root 'packaging\VsiCreativeSuite.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }
if ($BuildLegacyLauncher)
{
    & $iscc "/DStageRoot=$stage" "/DProjectRoot=$root" "/O$out" (Join-Path $root 'packaging\VsiCreativeSuiteLegacy.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Legacy launcher compilation failed.' }
}
Write-Host "Done. Updated VSI Creative Suite v0.2.1 installer in: $out"
