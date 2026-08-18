[CmdletBinding()]
param(
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src\PlexSleepGuard\PlexSleepGuard.csproj'
$publishDirectory = Join-Path $PSScriptRoot '.publish\win-x64'
$installDirectory = Join-Path $env:LOCALAPPDATA 'PlexSleepGuard'
$executable = Join-Path $installDirectory 'PlexSleepGuard.exe'
$taskName = 'PlexSleepGuard'

if (-not $SkipPublish) {
    dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $publishDirectory
}

if (-not (Test-Path $publishDirectory)) {
    throw "Publish output not found at $publishDirectory. Run without -SkipPublish first."
}

New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null
Copy-Item -LiteralPath (Join-Path $publishDirectory 'PlexSleepGuard.exe') -Destination $executable -Force

$action = New-ScheduledTaskAction -Execute $executable -WorkingDirectory $installDirectory
$trigger = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Days 7)
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null

Write-Host "Installed $executable"
Write-Host "Scheduled task '$taskName' will launch it at interactive logon."
Write-Host "Configure $([IO.Path]::Combine($installDirectory, 'config.json')) with your Plex token, then start the task or sign in again."
