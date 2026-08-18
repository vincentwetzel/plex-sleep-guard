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

From the repository root:

```powershell
dotnet run --project .\src\PlexSleepGuard\PlexSleepGuard.csproj -- --status
dotnet run --project .\src\PlexSleepGuard\PlexSleepGuard.csproj -- --test-power-request
dotnet run --project .\src\PlexSleepGuard\PlexSleepGuard.csproj -- --console
```

`--status` queries Plex and exits without creating a power request. `--test-power-request` holds the system-required request for about 60 seconds; inspect it from another terminal with `powercfg /requests`.

## Install and uninstall

Run from PowerShell in the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

The installer publishes a self-contained x64 single-file executable to `%LOCALAPPDATA%\PlexSleepGuard\` and registers a limited-permission Scheduled Task at interactive logon. It does not require elevation. Uninstall retains config and logs; use `-RemoveData` when you explicitly want those removed.

## Troubleshooting

Check `powercfg /requests` during a grace period and review the current day's log. Confirm Plex is listening on `127.0.0.1:32400`, the token is valid, and Windows or security software is not blocking local HTTP. More troubleshooting details are in [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md), and [agents.md](agents.md) for project guidance.
