param([string]$Path = (Join-Path $PSScriptRoot '..\catalog\apps.json'))
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing catalog: $Path" }
$catalog = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
if ($catalog.schemaVersion -ne 1) { throw 'Unsupported catalog schemaVersion' }
if ($catalog.channel -notin @('stable','beta')) { throw 'Invalid channel' }
if ($catalog.suiteVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid suiteVersion' }
if ($null -eq $catalog.apps -or $catalog.apps.Count -lt 1) { throw 'App list is empty' }
$seen = @{}
foreach ($app in $catalog.apps) {
  if ($app.id -notmatch '^[a-z][a-z0-9-]{1,31}$') { throw "Invalid app id: $($app.id)" }
  if ($seen.ContainsKey($app.id)) { throw "Duplicate app id: $($app.id)" }
  $seen[$app.id] = $true
  if ([string]::IsNullOrWhiteSpace($app.name) -or [string]::IsNullOrWhiteSpace($app.description) -or [string]::IsNullOrWhiteSpace($app.symbol)) { throw "Missing app display text: $($app.id)" }
  if ($app.executable -notmatch '^[A-Za-z0-9_.-]+\.exe$') { throw "Invalid executable name: $($app.id)" }
  if ($app.accent -notmatch '^#[0-9A-Fa-f]{6}$') { throw "Invalid accent: $($app.id)" }
  if ($app.delivery -notin @('bundled-installer','bundled-portable','release-zip')) { throw "Invalid delivery: $($app.id)" }
  if ($app.delivery -eq 'release-zip') {
    if ($null -eq $app.package) { throw "Missing release package: $($app.id)" }
    if ($app.package.version -notmatch '^\d+\.\d+\.\d+$' -or $app.package.sha256 -notmatch '^[0-9a-fA-F]{64}$') { throw "Invalid release package metadata: $($app.id)" }
    if ($app.package.assetName -notmatch '^[A-Za-z0-9_.-]+\.zip$') { throw "Invalid package assetName: $($app.id)" }
  }
}
Write-Host "Catalog validated: $($catalog.apps.Count) apps, suite v$($catalog.suiteVersion)"
