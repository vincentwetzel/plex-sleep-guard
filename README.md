# PlexSleepGuard

PlexSleepGuard is a small, headless, per-user Windows utility for Plex Media Server. It polls Plex's local `/status/sessions` API and, after the final active session ends, holds a Windows `PowerRequestSystemRequired` request during a configurable grace period. This covers the gap where Windows' idle timer has already expired while Plex was playing.

It does not simulate keyboard or mouse input, change Windows' last-input timestamp, inhibit the display, or install a Windows Service. It targets Windows x64 and ships as a self-contained single-file EXE.

Manual launches check GitHub for a newer stable release. A newer `PlexSleepGuard-Setup.exe` is downloaded, checked against GitHub's SHA-256 asset digest, installed, and started. Automatic logon launches use `--background` and skip the network check. If GitHub is unavailable, the current version continues running.

## Behavior

The monitor uses three states:

- `IDLE`: no relevant Plex session is active and no power request is held.
- `PLAYING`: one or more relevant Plex sessions are active.
- `GRACE_PERIOD`: the final session has ended; a system-required power request is held until the grace period expires.

Paused and buffering-like sessions count as active. A successful poll with no active sessions starts grace. A failed or timed-out poll does not mean playback ended and leaves the last known state unchanged. If playback resumes during grace, the request is cleared immediately. The request is also cleared on expiration, cancellation, and controlled shutdown; it never requests display-required behavior.

## Install

1. Download `PlexSleepGuard-Setup.exe` from the [GitHub Releases page](https://github.com/vincentwetzel/plex-sleep-guard/releases).
2. Run it on Windows. No administrator permission is required.
3. Paste the Plex token when prompted. Setup validates it, copies the EXE to `%LOCALAPPDATA%\PlexSleepGuard\PlexSleepGuard.exe`, registers the limited-permission `PlexSleepGuard` logon task, and starts the monitor.
4. Delete the downloaded setup EXE after setup reports the installed path.

The first interactive run also performs setup when no configuration exists. Setup and the installed app are the same executable; no script, runtime installation, or separate installer is required.

## Configuration

The first run creates `%LOCALAPPDATA%\PlexSleepGuard\config.json`:

```json
{
  "PlexServerUrl": "http://127.0.0.1:32400",
  "PlexToken": "put-your-token-here",
  "PollIntervalSeconds": 5,
  "GracePeriodMinutes": 15
}
```

Polling is clamped to 1–3600 seconds and grace to 0–1440 minutes. Invalid server URLs fall back to the localhost default, and trailing slashes are removed. The token is sent as `X-Plex-Token` and is never logged or printed by `--status`.

For isolated development or smoke tests, set `PLEX_SLEEP_GUARD_DATA_DIR` to a writable directory. This replaces `%LOCALAPPDATA%` as the parent of the `PlexSleepGuard` data directory for configuration and logs. Never commit a real token or put one in command history or issue reports. See [docs/CONFIGURATION.md](docs/CONFIGURATION.md).

## Command-line switches

Run these against the installed `PlexSleepGuard.exe` unless noted otherwise:

| Switch | Purpose |
| --- | --- |
| *(no switch)* | Start the monitor. A downloaded EXE installs itself first; an installed manual launch checks for updates. |
| `--status` | Poll Plex once, print non-secret configuration and active sessions, then exit. Exit code `0` means the request succeeded; `1` means it failed. No power request is created. |
| `--quiet` | With `--status`, suppress status output while retaining the exit code. |
| `--setup` | Prompt for a new token, validate Plex, install/update the EXE, and register automatic startup. |
| `--uninstall` | Remove the `PlexSleepGuard` logon task. Configuration, logs, and the EXE are retained. |
| `--test-power-request` | Hold a system-required request for 60 seconds; inspect it from another terminal with `powercfg /requests`. |
| `--console` | Allocate a console and mirror log output there. Combine with the monitor or diagnostics when troubleshooting. |
| `--background` | Internal logon-task mode. It starts the monitor without the manual-launch update check. |

`--apply-update`, `--source`, `--target`, and `--wait-pid` are internal updater arguments and are not intended for manual use.

## Updating and uninstalling

Manual launches update only when a newer stable GitHub release contains an asset named exactly `PlexSleepGuard-Setup.exe` with a SHA-256 digest. The update process replaces the installed EXE and restarts the background monitor. A failed check or failed update does not prevent the current installation from running.

To remove automatic startup while retaining local data:

```powershell
PlexSleepGuard.exe --uninstall
```

Delete `%LOCALAPPDATA%\PlexSleepGuard` manually only if you also want to remove the configuration and logs.

## Developers

Requirements are Windows x64 and the .NET 10 SDK. Build, test, and publish commands are in [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md). Architecture details, configuration guidance, and troubleshooting are documented in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/CONFIGURATION.md](docs/CONFIGURATION.md), and [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).
