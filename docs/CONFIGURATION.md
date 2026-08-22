# Configuration

PlexSleepGuard stores per-user data under `%LOCALAPPDATA%\PlexSleepGuard`:

- `config.json` contains the server address, token, polling interval, and grace period.
- `Logs\` contains daily plain-text logs with approximately seven days of best-effort retention.

The first run creates this configuration template:

```json
{
  "PlexServerUrl": "http://127.0.0.1:32400",
  "PlexToken": "xxxxxxxxxxxxxxxxxxxx",
  "PollIntervalSeconds": 5,
  "GracePeriodMinutes": 15
}
```

`PlexServerUrl` must be an absolute `http` or `https` URL. The default is `http://127.0.0.1:32400`; trailing slashes are removed. `PollIntervalSeconds` is normalized to 1–3600 seconds. `GracePeriodMinutes` is normalized to 0–1440 minutes. A malformed JSON file or unreadable configuration causes safe defaults to be recreated.

The token is sent in the `X-Plex-Token` header. It is intentionally excluded from logs, status output, source, and repository files. Treat it like a password. The easiest safe workflow is to run `PlexSleepGuard-Setup.exe`, which hides token input, saves it locally, tests Plex, and reports the installed path. To change it later:

```powershell
PlexSleepGuard.exe --setup
```

The token can be obtained from Plex Web or another Plex client by inspecting a request to the local server and copying its `X-Plex-Token` value. Do not place a real token in source control, command history, issue reports, or logs.

## Data-directory override

For development and smoke tests, set `PLEX_SLEEP_GUARD_DATA_DIR` to a writable parent directory:

```powershell
$env:PLEX_SLEEP_GUARD_DATA_DIR = 'C:\Temp\PlexSleepGuard-Test'
PlexSleepGuard.exe --status
```

The application then uses `<value>\PlexSleepGuard\config.json` and `<value>\PlexSleepGuard\Logs\`. When the variable is absent, `%LOCALAPPDATA%` is used. The variable must be set before starting the process.
