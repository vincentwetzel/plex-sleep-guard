# PlexSleepGuard

PlexSleepGuard is a small, headless per-user Windows utility for Plex Media Server. It polls Plex's local `/status/sessions` API and, only after the final active playback session ends, holds a Windows `PowerRequestSystemRequired` request for a configurable grace period. This covers the gap where Windows's idle timer has already expired while Plex was playing.

It never simulates keyboard or mouse input, never changes Windows's last-input timestamp, does not inhibit the display, and is not a Windows Service.

## Behavior

The application uses three states: `IDLE`, `PLAYING`, and `GRACE_PERIOD`. It enters `PLAYING` when one or more relevant Plex sessions exist, starts grace when the final session disappears, returns to `PLAYING` if playback resumes, and returns to `IDLE` when grace expires. Paused sessions are treated as active so a short pause does not make the machine immediately sleep. A failed poll does not count as playback ending.

## Configuration

On first launch, the application creates `%LOCALAPPDATA%\PlexSleepGuard\config.json`:

```json
{
  "PlexServerUrl": "http://127.0.0.1:32400",
  "PlexToken": "put-your-token-here",
  "PollIntervalSeconds": 5,
  "GracePeriodMinutes": 15
}
```

The token is never logged. A Plex token can be obtained from a Plex client or server request (see [docs/CONFIGURATION.md](docs/CONFIGURATION.md)); it must be placed only in the per-user config file and never committed.

Logs are plain text under `%LOCALAPPDATA%\PlexSleepGuard\Logs\`, with approximately seven days of retention.

## Run and diagnostics

From the installed or published EXE:

```powershell
PlexSleepGuard.exe --status
PlexSleepGuard.exe --status --quiet
PlexSleepGuard.exe --setup
PlexSleepGuard.exe --test-power-request
PlexSleepGuard.exe --console
```

Double-clicking the EXE the first time opens a short setup prompt, saves the token, installs automatic startup, and launches the background monitor. Later double-clicks confirm that it is running in the background; starting a second copy is prevented. If Windows renamed a downloaded file such as `PlexSleepGuard (1).exe`, it is copied to the normal installed location and the renamed copy exits. `--status` queries Plex and exits without creating a power request; run it as `PlexSleepGuard.exe --status` from PowerShell. Add `--quiet` to suppress status output while retaining the exit code. `--setup` changes the token. `--uninstall` removes automatic startup while retaining configuration and logs. `--test-power-request` holds the system-required request for about 60 seconds; inspect it from another terminal with `powercfg /requests`. `--background` is used by the logon task and is normally not run manually.

If upgrading from an older script-based installation, run `PlexSleepGuard.exe --setup` once so the EXE can replace the old installation and update the startup task.

## Install and uninstall

No installer or administrator permission is required. Copy the self-contained `PlexSleepGuard.exe` to a Windows PC and run it. Setup copies it to `%LOCALAPPDATA%\PlexSleepGuard\PlexSleepGuard.exe` and registers the limited-permission `PlexSleepGuard` task to start at logon.

To remove automatic startup while keeping configuration and logs:

```powershell
PlexSleepGuard.exe --uninstall
```

The installed EXE and its data directory can then be deleted manually if desired. `--uninstall` does not delete the token, configuration, or logs.

## For developers

```powershell
dotnet publish .\src\PlexSleepGuard\PlexSleepGuard.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\dist
```

Copy `dist\PlexSleepGuard.exe` to another Windows PC and double-click it. No source code, PowerShell scripts, .NET installation, or separate installer is required. The EXE copies itself to `%LOCALAPPDATA%\PlexSleepGuard\` and registers a per-user logon task.

## Troubleshooting

Check `powercfg /requests` during a grace period and review the current day's log. Confirm Plex is listening on `127.0.0.1:32400`, the token is valid, and Windows or security software is not blocking local HTTP. More troubleshooting details are in [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).

See [docs/CONFIGURATION.md](docs/CONFIGURATION.md), [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md), [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md), and [agents.md](agents.md) for more detail.
