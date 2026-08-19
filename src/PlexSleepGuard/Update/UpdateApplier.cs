using System.Diagnostics;

namespace PlexSleepGuard.Update;

internal static class UpdateApplier
{
    public static bool Start(string downloadedSetupPath, string installedExecutablePath, int processToWaitFor)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = downloadedSetupPath,
                WorkingDirectory = Path.GetDirectoryName(downloadedSetupPath),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            startInfo.ArgumentList.Add("--apply-update");
            startInfo.ArgumentList.Add("--source");
            startInfo.ArgumentList.Add(downloadedSetupPath);
            startInfo.ArgumentList.Add("--target");
            startInfo.ArgumentList.Add(installedExecutablePath);
            startInfo.ArgumentList.Add("--wait-pid");
            startInfo.ArgumentList.Add(processToWaitFor.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return Process.Start(startInfo) is not null;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryGetOption(args, "--source", out var source) ||
            !TryGetOption(args, "--target", out var target) ||
            !TryGetOption(args, "--wait-pid", out var waitPidText) ||
            !int.TryParse(waitPidText, out var waitPid) ||
            !File.Exists(source))
        {
            return 1;
        }

        await WaitForProcessToExitAsync(waitPid).ConfigureAwait(false);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                File.Copy(source, target, overwrite: true);
                TryDelete(source);
                await StartBackgroundAsync(target).ConfigureAwait(false);
                return 0;
            }
            catch (IOException) when (attempt < 19)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException) when (attempt < 19)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
            }
        }

        return 1;
    }

    private static async Task WaitForProcessToExitAsync(int processId)
    {
        for (var attempt = 0; attempt < 120; attempt++)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                if (process.HasExited)
                {
                    return;
                }
            }
            catch (ArgumentException)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        }
    }

    private static async Task StartBackgroundAsync(string executablePath)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
        if (TryStartDirect(executablePath))
        {
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            if (Process.GetProcessesByName("PlexSleepGuard").Length > 0)
            {
                return;
            }
        }

        _ = TryRunScheduledTask();
    }

    private static bool TryStartDirect(string executablePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = "--background"
        };
        try
        {
            using var process = Process.Start(startInfo);
            return process is not null;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static bool TryRunScheduledTask()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("/Run");
        startInfo.ArgumentList.Add("/TN");
        startInfo.ArgumentList.Add("PlexSleepGuard");

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static bool TryGetOption(string[] args, string option, out string value)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
            {
                value = args[index + 1];
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        value = string.Empty;
        return false;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The temporary file can be cleaned up by the OS later.
        }
        catch (UnauthorizedAccessException)
        {
            // Windows may keep the running updater executable locked until it exits.
        }
    }
}
