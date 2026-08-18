# Configuration

The first launch creates `%LOCALAPPDATA%\PlexSleepGuard\config.json`.

```json
{
  "PlexServerUrl": "http://127.0.0.1:32400",
  "PlexToken": "xxxxxxxxxxxxxxxxxxxx",
  "PollIntervalSeconds": 5,
  "GracePeriodMinutes": 15
}
```

The default bind address is localhost. The token is sent as the `X-Plex-Token` HTTP header and is intentionally excluded from logs, status output, source, and repository files.

The easiest way to configure it is to run `PlexSleepGuard.exe --setup`; it opens a short prompt, hides your pasted token, saves it, and tests it automatically. Treat the token like a password.

You can also enter it manually in `config.json` if needed.

Values are normalized at startup: polling is constrained to 1–3600 seconds and grace is constrained to 0–1440 minutes. A malformed server URL falls back to the localhost default, and trailing slashes are removed.

The application data directory is `%LOCALAPPDATA%\PlexSleepGuard`. For isolated development or smoke tests, set `PLEX_SLEEP_GUARD_DATA_DIR` to a writable directory; this overrides `%LOCALAPPDATA%` for configuration and logs.

The token can be obtained from Plex Web or another Plex client by inspecting a request to the local server and copying its `X-Plex-Token` value. Never put a real token in source control, command history, issue reports, or logs.
