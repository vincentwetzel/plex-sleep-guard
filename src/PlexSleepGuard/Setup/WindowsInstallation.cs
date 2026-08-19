using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Text;

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
        return process.ProcessName.StartsWith("PlexSleepGuard", StringComparison.OrdinalIgnoreCase);
    }

    public static bool RegisterAtLogon(string executablePath)
    {
        var taskXmlPath = Path.Combine(Path.GetTempPath(), $"PlexSleepGuard-{Guid.NewGuid():N}.xml");
        try
        {
            File.WriteAllText(taskXmlPath, CreateTaskXml(executablePath), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return RunScheduledTasks("/Create", "/F", "/TN", "PlexSleepGuard", "/XML", taskXmlPath);
        }
        finally
        {
            try
            {
                File.Delete(taskXmlPath);
            }
            catch (IOException)
            {
                // The temporary task definition can be cleaned up later.
            }
        }
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
        var creatingTask = arguments.Any(static argument => string.Equals(argument, "/Create", StringComparison.OrdinalIgnoreCase));
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        process.WaitForExit();
        return process.ExitCode == 0 || (creatingTask && TaskExists());
    }

    private static bool TaskExists()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("/Query");
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

    private static string CreateTaskXml(string executablePath)
    {
        var userSid = WindowsIdentity.GetCurrent().User?.Value;
        if (string.IsNullOrWhiteSpace(userSid))
        {
            throw new InvalidOperationException("Windows could not determine the current user.");
        }

        var escapedExecutablePath = SecurityElement.Escape(executablePath);
        var escapedWorkingDirectory = SecurityElement.Escape(Path.GetDirectoryName(executablePath) ?? string.Empty);
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <Description>PlexSleepGuard background Plex sleep monitor</Description>
              </RegistrationInfo>
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                </LogonTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{userSid}</UserId>
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>LeastPrivilege</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <AllowHardTerminate>true</AllowHardTerminate>
                <StartWhenAvailable>true</StartWhenAvailable>
                <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                <Priority>7</Priority>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{escapedExecutablePath}</Command>
                  <Arguments>--background</Arguments>
                  <WorkingDirectory>{escapedWorkingDirectory}</WorkingDirectory>
                </Exec>
              </Actions>
            </Task>
            """;
    }
}
