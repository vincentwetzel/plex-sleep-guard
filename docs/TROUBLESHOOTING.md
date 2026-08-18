# Troubleshooting

1. Run `dotnet run --project .\src\PlexSleepGuard\PlexSleepGuard.csproj -- --status`. It reports reachability, session count, session states, and non-secret configuration.
2. Check `%LOCALAPPDATA%\PlexSleepGuard\Logs\` for the current date. Look for HTTP status errors, XML parsing errors, and state transitions.
3. Confirm Plex Media Server is running locally and that `http://127.0.0.1:32400/identity` responds.
4. Confirm the token in `config.json` is current. A 401/403 response generally means the token is absent or invalid.
5. During grace, run `powercfg /requests`. The reason should include `PlexSleepGuard post-playback grace period` and only system-required behavior should be present.

If Plex is unavailable, the process remains alive and retries at the configured interval. A failed request never directly starts grace or clears an existing request. The grace timer can still expire normally based on its already-recorded deadline.
