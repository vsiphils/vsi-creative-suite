param([string]$Path = (Join-Path $PSScriptRoot '..\catalog\apps.json'))
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing catalog: $Path" }
$catalog = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
if ($catalog.schemaVersion -ne 1) { throw 'Unsupported catalog schemaVersion' }
if ($catalog.channel -notin @('stable','beta')) { throw 'Invalid channel' }
if ($catalog.suiteVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid suiteVersion' }
if ($null -eq $catalog.apps -or $catalog.apps.Count -lt 1 -or $catalog.apps.Count -gt 24) { throw 'Catalog must contain between 1 and 24 approved apps' }
$seen = @{}
foreach ($app in $catalog.apps) {
  if ($app.id -notmatch '^[a-z][a-z0-9-]{1,31}$') { throw "Invalid app id: $($app.id)" }
  if ($seen.ContainsKey($app.id)) { throw "Duplicate app id: $($app.id)" }
  $seen[$app.id] = $true
  if ([string]::IsNullOrWhiteSpace($app.name) -or [string]::IsNullOrWhiteSpace($app.description) -or [string]::IsNullOrWhiteSpace($app.symbol)) { throw "Missing app display text: $($app.id)" }
  if ($app.executable -notmatch '^[A-Za-z0-9_.-]+\.exe$') { throw "Invalid executable name: $($app.id)" }
  if ($app.accent -notmatch '^#[0-9A-Fa-f]{6}$') { throw "Invalid accent: $($app.id)" }
  if ($app.delivery -notin @('bundled-installer','bundled-portable','release-zip')) { throw "Invalid delivery: $($app.id)" }
  if ($app.repository -notmatch '^[A-Za-z0-9-]+/[A-Za-z0-9._-]+$') { throw "Invalid GitHub repository: $($app.id)" }
  if ($app.assetNameTemplate -notmatch '\{version\}') { throw "Missing version placeholder: $($app.id)" }
  $filename = $app.assetNameTemplate.Replace('{version}','1.2.3')
  if ($filename -notmatch '^[A-Za-z0-9._-]+\.(zip|exe)$') { throw "Invalid release asset pattern: $($app.id)" }
  if ($app.id -ne 'hew' -and $filename -notmatch '\.zip$') { throw "Only portable ZIP apps supported: $($app.id)" }
  if ($app.id -eq 'hew' -and $app.delivery -ne 'bundled-installer') { throw 'Hew requires official installer' }
}
Write-Host "Catalog validated: $($catalog.apps.Count) apps, suite v$($catalog.suiteVersion)"
