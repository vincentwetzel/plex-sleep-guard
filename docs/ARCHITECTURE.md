# Architecture

PlexSleepGuard is a single-process, per-user Windows application targeting `net10.0-windows` on x64. It has no GUI framework, Windows Service, inbound listener, input simulation, or last-input manipulation.

## Components

- `Program` owns argument handling, startup, diagnostics, cancellation, and the polling lifetime.
- `SetupWizard` handles first launch and `--setup` inside the EXE, including hidden token input and the initial Plex check.
- `WindowsInstallation` copies the EXE to the per-user data directory and creates or removes the least-privilege logon task through Task Scheduler.
- `AppConfiguration` loads JSON, applies safe defaults, normalizes values, and honors `PLEX_SLEEP_GUARD_DATA_DIR`.
- `FileLog` writes daily plain-text logs and performs approximately seven-day best-effort retention.
- `PlexMonitor` serializes one HTTP request to `/status/sessions`, sends `X-Plex-Token` when configured, bounds each poll, and parses direct XML media children.
- `PlaybackStateMachine` is pure state/timing logic. It receives only successful observations; failed polls leave the last known state untouched.
- `WindowsPowerManager` is the native boundary. It uses a safe handle with `PowerCreateRequest`, `PowerSetRequest`, and `PowerClearRequest`, requesting `PowerRequestSystemRequired` only.
- `GitHubReleaseUpdater` checks the latest stable release on manual launch, validates the named setup asset's SHA-256 digest, and hands off replacement to `UpdateApplier`.

## Session policy

The parser considers direct XML media children with a media `type`. Sessions whose state is not `stopped` are active, including `paused`, `playing`, and buffering-like states. Only active sessions are returned to the state machine. This prevents a temporary poll failure or short pause from being interpreted as playback ending.

## State and power lifecycle

The normal power lease exists in `PLAYING` and remains active through `GRACE_PERIOD`. A successful active observation transitions to `PLAYING` and acquires the system-required request before the idle timer can expire. A successful empty-session observation transitions from `PLAYING` to grace and retains the request. A successful active observation during grace transitions back to `PLAYING` and retains it. Expiration transitions to `IDLE` and releases it. The monitor also releases the lease in its `finally` path, so cancellation and controlled failures clear the request.

The separate `--test-power-request` path intentionally holds the same system-required request for 60 seconds. Status mode performs one poll and never creates a request.

## Startup and updates

Setup saves configuration, copies the current EXE to `%LOCALAPPDATA%\PlexSleepGuard\PlexSleepGuard.exe`, and registers an `ONLOGON` task running at least privilege with `--background`. Background launches skip GitHub checks. Manual launches can download a newer release, verify its digest, replace the installed EXE through a short-lived updater, and restart the background monitor. `--uninstall` removes only the task; configuration and logs remain.
