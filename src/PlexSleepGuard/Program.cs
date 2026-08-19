using PlexSleepGuard.Configuration;
using PlexSleepGuard.Plex;
using PlexSleepGuard.Power;
using PlexSleepGuard.Setup;
using PlexSleepGuard.State;
using System.Runtime.InteropServices;

namespace PlexSleepGuard;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var console = args.Any(static argument => string.Equals(argument, "--console", StringComparison.OrdinalIgnoreCase));
        var status = args.Any(static argument => string.Equals(argument, "--status", StringComparison.OrdinalIgnoreCase));
        var quietStatus = args.Any(static argument => string.Equals(argument, "--quiet", StringComparison.OrdinalIgnoreCase));
        var testPower = args.Any(static argument => string.Equals(argument, "--test-power-request", StringComparison.OrdinalIgnoreCase));
        var setup = args.Any(static argument => string.Equals(argument, "--setup", StringComparison.OrdinalIgnoreCase));
        var uninstall = args.Any(static argument => string.Equals(argument, "--uninstall", StringComparison.OrdinalIgnoreCase));
        var background = args.Any(static argument => string.Equals(argument, "--background", StringComparison.OrdinalIgnoreCase));
        var normalLaunch = args.Length == 0;

        if (uninstall)
        {
            ConsoleMode.EnsureConsole();
            WindowsInstallation.StopOtherInstances();
            var removed = WindowsInstallation.RemoveAtLogon();
            Console.WriteLine(removed
                ? "Automatic startup removed. You can delete this EXE if you no longer need it."
                : "Automatic startup was not registered. You can delete this EXE if you no longer need it.");
            return removed ? 0 : 1;
        }

        if (setup || (!status && !testPower && !background && !File.Exists(AppConfiguration.FilePath)))
        {
            return await SetupWizard.RunAsync().ConfigureAwait(false);
        }

        if (normalLaunch && !WindowsInstallation.IsRunningFromInstalledExecutable())
        {
            WindowsInstallation.StopOtherInstances();
            var installedPath = WindowsInstallation.EnsureInstalledExecutable();
            var registered = WindowsInstallation.RegisterAtLogon(installedPath);
            WindowsInstallation.StartInstalled(installedPath);
            UserNotification.ShowInformation(registered
                ? "PlexSleepGuard is now running in the background.\n\n" +
                  $"Installed application:\n{installedPath}\n\n" +
                  "You can close this message and delete the downloaded setup EXE. " +
                  "PlexSleepGuard will start automatically when you sign in."
                : "PlexSleepGuard is running in the background.\n\n" +
                  "It could not update automatic startup, but it is working for now.");
            return registered ? 0 : 1;
        }

        using var instanceGuard = !status && !testPower
            ? SingleInstanceGuard.TryAcquire()
            : null;
        if (!status && !testPower && instanceGuard is null)
        {
            if (!background)
            {
                UserNotification.ShowInformation(
                    "PlexSleepGuard is already running.\n\n" +
                    "Only one copy is needed. It is monitoring Plex in the background.");
            }

            return 0;
        }

            if (normalLaunch)
            {
                UserNotification.ShowInformation(
                    "PlexSleepGuard is now running in the background.\n\n" +
                    "It will monitor Plex and help Windows sleep after playback ends.\n\n" +
                    "You can close this message; nothing else is needed.");
            }

        if (console || (status && !quietStatus) || testPower)
        {
            ConsoleMode.EnsureConsole();
        }

        using var log = new FileLog(console || (status && !quietStatus) || testPower);
        log.Information("PlexSleepGuard starting.");
        var configuration = AppConfiguration.Load(log);

        try
        {
            if (status)
            {
                return await RunStatusAsync(configuration, log, !quietStatus).ConfigureAwait(false);
            }

            if (testPower)
            {
                return await RunPowerTestAsync(log).ConfigureAwait(false);
            }

            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };
            await RunGuardAsync(configuration, log, shutdown.Token).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            log.Information("Shutdown requested.");
            return 0;
        }
        catch (Exception exception)
        {
            log.Failure("Controlled shutdown after an unexpected error.", exception);
            return 1;
        }
        finally
        {
            log.Information("PlexSleepGuard stopped.");
        }
    }

    private static async Task RunGuardAsync(AppConfiguration configuration, FileLog log, CancellationToken cancellationToken)
    {
        using var monitor = new PlexMonitor(configuration, log);
        IPowerManager powerManager = new WindowsPowerManager();
        using var powerLease = new PowerLeaseController(powerManager, log);
        var machine = new PlaybackStateMachine(TimeSpan.FromMinutes(configuration.GracePeriodMinutes));
        DateTimeOffset? lastGraceLog = null;
        log.Information("Monitor started. Paused Plex sessions are treated as active; transient polling failures preserve the last known state.");

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await monitor.PollAsync(cancellationToken).ConfigureAwait(false);
                if (result.Success)
                {
                    var active = result.Sessions.Any(static session => session.IsActive);
                    ApplyTransition(machine.ObserveActive(active, DateTimeOffset.Now), powerLease, log, ref lastGraceLog);
                }

                ApplyTransition(machine.Advance(DateTimeOffset.Now), powerLease, log, ref lastGraceLog);
                LogGraceRemaining(machine, log, ref lastGraceLog);
                await Task.Delay(TimeSpan.FromSeconds(configuration.PollIntervalSeconds), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            powerLease.Release();
            log.Information("Monitor stopped and any system sleep request was cleared.");
        }
    }

    private static void ApplyTransition(PlaybackTransition? transition, PowerLeaseController powerLease, FileLog log, ref DateTimeOffset? lastGraceLog)
    {
        if (transition is null)
        {
            return;
        }

        log.Information($"Playback state transition: {transition.From} -> {transition.To}.");
        switch (transition.To)
        {
            case PlaybackState.Playing:
                if (transition.From == PlaybackState.Idle)
                {
                    log.Information("PLAYING detected.");
                }
                else
                {
                    log.Information("Playback resumed; grace-period sleep inhibition released.");
                }

                powerLease.Release();
                lastGraceLog = null;
                break;
            case PlaybackState.GracePeriod:
                log.Information($"Playback ended; grace period started and will end at {transition.GraceEndsAt:O}.");
                powerLease.Acquire();
                lastGraceLog = transition.At;
                break;
            case PlaybackState.Idle:
                log.Information("Grace period expired; returning to IDLE.");
                powerLease.Release();
                lastGraceLog = null;
                break;
        }
    }

    private static void LogGraceRemaining(PlaybackStateMachine machine, FileLog log, ref DateTimeOffset? lastGraceLog)
    {
        if (machine.State != PlaybackState.GracePeriod || machine.GraceEndsAt is null)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        if (lastGraceLog is null || now - lastGraceLog.Value >= TimeSpan.FromMinutes(1))
        {
            var remaining = machine.GraceEndsAt.Value - now;
            if (remaining > TimeSpan.Zero)
            {
                log.Information($"Grace period remaining: {Math.Ceiling(remaining.TotalMinutes)} minute(s).");
                lastGraceLog = now;
            }
        }
    }

    private static async Task<int> RunStatusAsync(AppConfiguration configuration, ILog log, bool printOutput)
    {
        using var monitor = new PlexMonitor(configuration, log);
        var result = await monitor.PollAsync(CancellationToken.None).ConfigureAwait(false);
        if (printOutput)
        {
            Console.WriteLine($"Plex server: {configuration.PlexServerUrl}");
            Console.WriteLine($"Poll interval: {configuration.PollIntervalSeconds}s");
            Console.WriteLine($"Grace period: {configuration.GracePeriodMinutes}m");
            Console.WriteLine($"Token configured: {!string.IsNullOrWhiteSpace(configuration.PlexToken)}");
            Console.WriteLine($"Reachable: {result.Success}");
        }

        if (!result.Success)
        {
            if (printOutput)
            {
                Console.WriteLine($"Error: {result.Error}");
            }

            return 1;
        }

        if (printOutput)
        {
            Console.WriteLine($"Playback sessions: {result.Sessions.Count}");
            foreach (var session in result.Sessions)
            {
                Console.WriteLine($"- {session.Title} ({session.Type}): {session.State}");
            }
        }

        return 0;
    }

    private static async Task<int> RunPowerTestAsync(ILog log)
    {
        using var lease = new PowerLeaseController(new WindowsPowerManager(), log);
        lease.Acquire();
        Console.WriteLine("System-required power request is active. Run 'powercfg /requests' in another terminal.");
        Console.WriteLine("Holding the request for 60 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        lease.Release();
        Console.WriteLine("System-required power request cleared.");
        return 0;
    }

    private sealed class PowerLeaseController : IDisposable
    {
        private readonly IPowerManager manager;
        private readonly ILog log;
        private IDisposable? lease;

        public PowerLeaseController(IPowerManager manager, ILog log)
        {
            this.manager = manager;
            this.log = log;
        }

        public void Acquire()
        {
            if (lease is not null)
            {
                return;
            }

            lease = manager.AcquireSystemRequired("PlexSleepGuard post-playback grace period");
            log.Information("Windows power request created and set (system required only; display is not inhibited).");
        }

        public void Release()
        {
            var current = Interlocked.Exchange(ref lease, null);
            if (current is null)
            {
                return;
            }

            current.Dispose();
            log.Information("Windows power request cleared.");
        }

        public void Dispose() => Release();
    }

    internal static class ConsoleMode
    {
        public static void EnsureConsole()
        {
            if (!OperatingSystem.IsWindows() || GetConsoleWindow() != IntPtr.Zero)
            {
                return;
            }

            _ = AllocConsole();
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
    }
}
