# Architecture

PlexSleepGuard is a single-process, per-user Windows application with no GUI framework and no service registration.

## Components

- `Program` owns startup, CLI diagnostics, cancellation, and the polling lifetime.
- `AppConfiguration` reads and normalizes JSON under `%LOCALAPPDATA%\PlexSleepGuard`.
- `FileLog` writes daily plain-text files and performs seven-day best-effort retention.
- `PlexMonitor` performs one serialized HTTP request to `/status/sessions`, sends `X-Plex-Token`, and parses the XML `MediaContainer` children.
- `PlaybackStateMachine` is pure state/timing logic. It only receives a successful active-session observation; a failed poll leaves the last known state untouched.
- `WindowsPowerManager` is the native boundary. It uses `PowerCreateRequest`, `PowerSetRequest`, and `PowerClearRequest` with a `SafeHandle` and `PowerRequestSystemRequired` only.

The power lease is held only while the state machine is in `GRACE_PERIOD`. It is released on resume, expiration, normal cancellation, and controlled shutdown.

## Session policy

Direct media children with a media `type` are considered sessions. Any state other than `stopped` is considered active, including `paused`, `playing`, and buffering-like states. This makes the final-session transition conservative when Plex is temporarily unavailable or a user pauses briefly.
