[CmdletBinding(SupportsShouldProcess)]
param(
    [switch]$RemoveData
)

$ErrorActionPreference = 'Stop'
$taskName = 'PlexSleepGuard'
$installDirectory = Join-Path $env:LOCALAPPDATA 'PlexSleepGuard'

Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
$applicationFiles = @('PlexSleepGuard.exe', 'PlexSleepGuard.pdb', 'PlexSleepGuard.runtimeconfig.json', 'PlexSleepGuard.deps.json')
foreach ($file in $applicationFiles) {
    $path = Join-Path $installDirectory $file
    if (Test-Path $path) { Remove-Item -LiteralPath $path -Force }
}

if ($RemoveData) {
    Remove-Item -LiteralPath (Join-Path $installDirectory 'config.json') -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $installDirectory 'Logs') -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Scheduled task removed and installed application files deleted."
if (-not $RemoveData) { Write-Host "Configuration and logs were retained under $installDirectory. Use -RemoveData to delete them." }
