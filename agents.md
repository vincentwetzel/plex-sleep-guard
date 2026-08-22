# Agent and contributor guidance

- Target .NET 10, Windows x64, and keep the application dependency-light.
- The user-facing download is the self-contained single-file `PlexSleepGuard-Setup.exe`; setup installs the runtime as `PlexSleepGuard.exe` under `%LOCALAPPDATA%\PlexSleepGuard`.
- Do not add a GUI framework, Windows Service, inbound listener, input simulation, or last-input manipulation.
- Never hardcode, print, or commit Plex tokens. Use `%LOCALAPPDATA%\PlexSleepGuard\config.json` for local credentials.
- Power inhibition is allowed in `PLAYING` and `GRACE_PERIOD`, must use system-required behavior without display-required behavior, and must be cleared on every exit path.
- Preserve the conservative polling rule: failed Plex requests do not mean playback ended.
- Run Release build and tests after implementation changes.
- Keep tests deterministic and avoid invoking native Windows power APIs from unit tests.
- Update README/docs when behavior, configuration, installation, or troubleshooting changes.
- Keep the README and files under `docs/` synchronized with the shipped EXE; document supported command-line switches and any environment-variable overrides.
