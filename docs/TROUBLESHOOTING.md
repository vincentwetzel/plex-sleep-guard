# Troubleshooting

1. Run `PlexSleepGuard.exe --status`. It reports reachability, active session count, session states, and non-secret configuration. The exit code is `0` when the Plex request succeeds and `1` when it fails.
2. Check `%LOCALAPPDATA%\PlexSleepGuard\Logs\` for the current date. Look for HTTP status errors, XML parsing errors, and state transitions.
3. Confirm Plex Media Server is running locally and that `http://127.0.0.1:32400/identity` responds.
4. Confirm the token in `config.json` is current. A 401/403 response generally means the token is absent or invalid.
5. During grace, run `powercfg /requests`. The reason should include `PlexSleepGuard post-playback grace period` and only system-required behavior should be present.

If setup reports that Plex is reachable but automatic startup could not be created, run the installed EXE manually and check that Task Scheduler is available. The task is created with limited permissions and does not require elevation. In Task Scheduler, the `PlexSleepGuard` task should show an `At log on` trigger and a successful last run result (`0x0`).

Manual launches check GitHub for a newer stable release and update automatically when one is available. The update is verified before installation and the background monitor is restarted afterward. If an update appears to finish but PlexSleepGuard is not running, launch `%LOCALAPPDATA%\PlexSleepGuard\PlexSleepGuard.exe` once; the current installed version will continue working even when GitHub cannot be reached. Review the Windows Application event log if the installed EXE reports an application error.

If Plex is unavailable, the process remains alive and retries at the configured interval. A failed request never directly starts grace or clears an existing request. The grace timer can still expire normally based on its already-recorded deadline.

If playback resumes during the grace period, the request is cleared immediately. Paused sessions count as active, so pausing briefly does not start grace.
