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

To obtain a token, sign in to Plex Web, open a media item, and inspect a request made to the local Plex server in the browser's developer tools. The `X-Plex-Token` query/header value is the token to copy into `config.json`. Treat it like a password.

Values are normalized at startup: polling is constrained to 1–3600 seconds and grace is constrained to 0–1440 minutes. A malformed server URL falls back to the localhost default.
