using PlexSleepGuard.Configuration;
using PlexSleepGuard.Plex;
using System.Text;

namespace PlexSleepGuard.Setup;

internal static class SetupWizard
{
    public static async Task<int> RunAsync()
    {
        Program.ConsoleMode.EnsureConsole();
        using var log = new FileLog(true);
        var configuration = AppConfiguration.Load(log);

        Console.WriteLine();
        Console.WriteLine("PlexSleepGuard setup");
        Console.WriteLine("====================");
        if (string.IsNullOrWhiteSpace(configuration.PlexToken))
        {
            Console.WriteLine("Paste your Plex token below. Input will be hidden.");
            Console.WriteLine("Press Enter without a token to quit setup.");
        }
        else
        {
            Console.WriteLine("A Plex token is already saved. Press Enter to keep it, or paste a replacement.");
        }

        var token = ReadHidden("Plex token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            configuration.PlexToken = token.Trim();
        }

        if (string.IsNullOrWhiteSpace(configuration.PlexToken))
        {
            Console.WriteLine("No token entered. Setup was cancelled.");
            return 2;
        }

        configuration.Save();
        Console.WriteLine("Token saved locally. Checking Plex...");
        using var monitor = new PlexMonitor(configuration, log);
        var result = await monitor.PollAsync(CancellationToken.None).ConfigureAwait(false);
        if (!result.Success)
        {
            Console.WriteLine($"Plex check failed: {result.Error}");
            return 1;
        }

        WindowsInstallation.StopOtherInstances();
        var installedPath = WindowsInstallation.EnsureInstalledExecutable();
        if (!WindowsInstallation.RegisterAtLogon(installedPath))
        {
            Console.WriteLine("Plex works, but Windows could not create automatic startup. You can still run the EXE manually.");
            return 1;
        }

        Console.WriteLine("Plex accepted the token.");
        Console.WriteLine($"Installed application: {installedPath}");
        Console.WriteLine("PlexSleepGuard will run automatically when you sign in.");
        Console.WriteLine("You can now delete the downloaded setup EXE.");
        WindowsInstallation.StartInstalled(installedPath);
        return 0;
    }

    private static string ReadHidden(string prompt)
    {
        Console.Write($"{prompt}: ");
        var value = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0)
                {
                    value.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (char.IsControl(key.KeyChar))
            {
                continue;
            }

            value.Append(key.KeyChar);
            Console.Write('*');
        }

        Console.WriteLine();
        return value.ToString();
    }
}
