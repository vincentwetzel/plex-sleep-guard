# Development

Requirements: Windows x64 and the .NET 10 SDK.

## Build and test

```powershell
dotnet restore .\src\PlexSleepGuard\PlexSleepGuard.csproj --ignore-failed-sources
dotnet restore .\tests\PlexSleepGuard.Tests\PlexSleepGuard.Tests.csproj --ignore-failed-sources
dotnet build .\src\PlexSleepGuard\PlexSleepGuard.csproj -c Release --no-restore
dotnet test .\tests\PlexSleepGuard.Tests\PlexSleepGuard.Tests.csproj -c Release --no-build
```

The solution targets `net10.0-windows` and x64. Run the commands on Windows with the .NET 10 SDK. Tests for the state machine and XML parsing are deterministic and do not invoke native Windows power APIs. Keep native calls behind `IPowerManager`; use `--test-power-request` only for an intentional machine-level diagnostic.

The production project is a Windows GUI-subsystem process, so normal startup has no console window. `--console` allocates one and mirrors log output there. For an isolated smoke test, set `PLEX_SLEEP_GUARD_DATA_DIR` to a writable location before launching the EXE.

## Publish

Create the self-contained single-file build:

```powershell
dotnet publish .\src\PlexSleepGuard\PlexSleepGuard.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o .\dist
Rename-Item .\dist\PlexSleepGuard.exe PlexSleepGuard-Setup.exe
```

`PlexSleepGuard-Setup.exe` is the only file a user downloads. It installs itself as `PlexSleepGuard.exe` under `%LOCALAPPDATA%\PlexSleepGuard` and registers the per-user logon task. Do not commit generated output from `bin`, `obj`, `.publish`, or `dist`.

## Release checklist

1. Update `<Version>` in `src/PlexSleepGuard/PlexSleepGuard.csproj`.
2. Run the Release build and tests.
3. Publish the self-contained EXE to `dist` and rename it to `PlexSleepGuard-Setup.exe`.
4. Create a matching GitHub release/tag and upload `PlexSleepGuard-Setup.exe` as the user download.

The updater expects the latest stable GitHub release to contain an asset named exactly `PlexSleepGuard-Setup.exe` and a SHA-256 asset digest. The GitHub Actions workflow performs the Release test, publish, rename, and artifact upload for CI builds.
