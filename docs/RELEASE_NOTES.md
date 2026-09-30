# Release notes

## v0.1.14

- Added periodic `powercfg /requests` snapshots while playback or the grace period is protected.
- Added poll outcome, lease-status, last-successful-poll, and monitor-loop-gap logging.
- Added troubleshooting guidance for diagnosing Windows sleep and process suspension.
- Updated the release checklist and documentation for the self-contained `PlexSleepGuard-Setup.exe`.

The setup EXE is self-contained for Windows x64 and installs the runtime under `%LOCALAPPDATA%\PlexSleepGuard`.
