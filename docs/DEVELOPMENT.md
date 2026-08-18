# Development

Requirements: Windows x64 and the .NET 10 SDK.

Restore, build, and test:

```powershell
dotnet restore .\src\PlexSleepGuard\PlexSleepGuard.csproj --ignore-failed-sources
dotnet restore .\tests\PlexSleepGuard.Tests\PlexSleepGuard.Tests.csproj --ignore-failed-sources
dotnet build .\src\PlexSleepGuard\PlexSleepGuard.csproj -c Release --no-restore
dotnet test .\tests\PlexSleepGuard.Tests\PlexSleepGuard.Tests.csproj -c Release --no-build
```

The solution targets `net10.0-windows` and x64. Run the build and tests from Windows with the .NET 10 SDK installed.

For a self-contained single-file release:

```powershell
dotnet publish .\src\PlexSleepGuard\PlexSleepGuard.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\.publish\win-x64
```

For a user-ready artifact, use `-o .\dist` instead. The resulting `PlexSleepGuard.exe` is the only file another Windows user needs.

The state-machine and XML parsing tests do not invoke native power APIs. Keep native calls behind `IPowerManager`; use `--test-power-request` for an intentional machine-level diagnostic. Normal production startup is a Windows GUI-subsystem process with no console window; `--console` enables a console and mirrors logs there.

For an isolated local smoke test, set `PLEX_SLEEP_GUARD_DATA_DIR` to a writable directory. Production uses `%LOCALAPPDATA%` when this variable is absent.

The first interactive run invokes the in-process setup wizard. It validates the token, copies the EXE into the per-user application directory, and registers the logon task. The scheduled task starts the installed EXE with `--background`. Do not commit generated output from `bin`, `obj`, `.publish`, or `dist`.
