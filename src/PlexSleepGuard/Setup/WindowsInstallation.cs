using System.Diagnostics;

namespace PlexSleepGuard.Setup;

internal static class WindowsInstallation
{
    public static string InstalledExecutablePath => Path.Combine(
        Configuration.AppConfiguration.DirectoryPath, "PlexSleepGuard.exe");

    public static string EnsureInstalledExecutable()
    {
        var current = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(current))
        {
            throw new InvalidOperationException("Windows could not determine the current executable path.");
        }

        Directory.CreateDirectory(Configuration.AppConfiguration.DirectoryPath);
        if (!string.Equals(Path.GetFullPath(current), Path.GetFullPath(InstalledExecutablePath), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(current, InstalledExecutablePath, true);
        }

        return InstalledExecutablePath;
    }

    public static bool IsRunningFromInstalledExecutable()
    {
        var current = Environment.ProcessPath;
        return !string.IsNullOrWhiteSpace(current) &&
               string.Equals(Path.GetFullPath(current), Path.GetFullPath(InstalledExecutablePath), StringComparison.OrdinalIgnoreCase);
    }

    public static void StopOtherInstances()
    {
        var currentId = Environment.ProcessId;
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (process.Id == currentId || !IsPlexSleepGuardProcess(process))
                {
                    continue;
                }

                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
                catch (InvalidOperationException)
                {
                    // It exited between enumeration and termination.
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // A process we do not own cannot be stopped; the later copy reports a clear error.
                }
            }
        }
    }

    private static bool IsPlexSleepGuardProcess(Process process)
    {
        return string.Equals(process.ProcessName, "PlexSleepGuard", StringComparison.OrdinalIgnoreCase) ||
               process.ProcessName.StartsWith("PlexSleepGuard (", StringComparison.OrdinalIgnoreCase);
    }

    public static bool RegisterAtLogon(string executablePath)
    {
        var taskAction = $"\"{executablePath}\" --background";
        return RunScheduledTasks(
            "/Create", "/F", "/SC", "ONLOGON", "/TN", "PlexSleepGuard", "/TR", taskAction, "/RL", "LIMITED");
    }

    public static bool RemoveAtLogon() => RunScheduledTasks(
        "/Delete", "/TN", "PlexSleepGuard", "/F");

    public static void StartInstalled(string executablePath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = "--background",
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static bool RunScheduledTasks(params string[] arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Arguments = string.Join(" ", arguments.Select(QuoteArgument))
        });
        if (process is null)
        {
            return false;
        }

        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static string QuoteArgument(string argument) => $"\"{argument.Replace("\"", "\\\"")}\"";
}
