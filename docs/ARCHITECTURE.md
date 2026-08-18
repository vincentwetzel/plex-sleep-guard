# Architecture

PlexSleepGuard is a single-process, per-user Windows application with no GUI framework and no service registration.

## Components

- `Program` owns startup, CLI diagnostics, cancellation, and the polling lifetime.
- `SetupWizard` handles first launch and `--setup` directly inside the EXE, so end users do not need PowerShell scripts.
- `WindowsInstallation` copies the EXE to the per-user application directory and registers/removes the limited-permission logon task through Windows Task Scheduler.
- `AppConfiguration` reads and normalizes JSON under `%LOCALAPPDATA%\PlexSleepGuard`.
- `FileLog` writes daily plain-text files and performs seven-day best-effort retention.
- `PlexMonitor` performs one serialized HTTP request to `/status/sessions`, sends `X-Plex-Token` when configured, and parses direct XML media children.
- `PlaybackStateMachine` is pure state/timing logic. It only receives a successful active-session observation; a failed poll leaves the last known state untouched.
- `WindowsPowerManager` is the native boundary. It uses `PowerCreateRequest`, `PowerSetRequest`, and `PowerClearRequest` with a `SafeHandle` and `PowerRequestSystemRequired` only.

The normal monitor power lease is held only while the state machine is in `GRACE_PERIOD`. It is released on resume, expiration, normal cancellation, and controlled shutdown. The separate `--test-power-request` diagnostic intentionally holds the same system-required request for 60 seconds.

## Session policy

Direct media children with a media `type` are considered sessions. Only sessions whose state is not `stopped` are returned by the parser and considered active, including `paused`, `playing`, and buffering-like states. This makes the final-session transition conservative when Plex is temporarily unavailable or a user pauses briefly.

## Lifecycle

The setup wizard runs inside the EXE, saves configuration, verifies Plex, copies the current EXE to `%LOCALAPPDATA%\PlexSleepGuard`, and registers a limited-permission `ONLOGON` scheduled task. The task launches the installed EXE with `--background`. Uninstall removes that task but deliberately retains configuration and logs.
