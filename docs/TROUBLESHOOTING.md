# Troubleshooting

Start with a one-shot status check:

```powershell
PlexSleepGuard.exe --status
```

It reports the server URL, normalized intervals, whether a token is configured, reachability, and active sessions. It never prints the token and never creates a power request. Exit code `0` means the Plex request succeeded; `1` means it failed. Add `--quiet` when only the exit code is needed.

Check `%LOCALAPPDATA%\PlexSleepGuard\Logs\` for the current day's log. Relevant entries include HTTP errors, XML/network errors, state transitions, grace-period progress, and power-request creation or release. A poll has a bounded timeout; a failed or timed-out request preserves the last known playback state and does not start grace by itself.

Confirm that Plex is running and that this URL responds:

```powershell
Invoke-WebRequest http://127.0.0.1:32400/identity
```

A 401 or 403 response usually means that the token is missing or invalid. Update it with `PlexSleepGuard.exe --setup`. Check that the configured server URL is reachable and that Windows or security software is not blocking local HTTP.

During grace, run this from another terminal:

```powershell
powercfg /requests
```

The request reason should include `PlexSleepGuard post-playback grace period`. Only system-required behavior is requested; the display is not inhibited. `--test-power-request` provides a controlled 60-second diagnostic for the same native power path:

```powershell
PlexSleepGuard.exe --test-power-request
```

If setup reports that Plex works but automatic startup could not be created, run the installed EXE manually and confirm that Task Scheduler is available. The `PlexSleepGuard` task should have an `At log on` trigger, run at least privilege, and launch the installed EXE with `--background`. No elevation is required.

Manual launches may check GitHub for a newer stable release. The downloaded asset is verified before installation, and the monitor is restarted afterward. If an update appears to finish but the monitor is not running, launch `%LOCALAPPDATA%\PlexSleepGuard\PlexSleepGuard.exe` once. GitHub being unavailable does not disable the installed version. Review the Windows Application event log if Windows reports an application error.

If playback resumes during grace, the request is cleared immediately. Paused sessions count as active, so a short pause does not start grace. The request is also cleared when grace expires or the process shuts down.
