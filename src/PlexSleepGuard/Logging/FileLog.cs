using System.Globalization;
using PlexSleepGuard.Configuration;

namespace PlexSleepGuard;

public sealed class FileLog : ILog
{
    private readonly object sync = new();
    private readonly string directory;
    private readonly bool console;
    private bool disposed;

    public FileLog(bool console)
    {
        this.console = console;
        directory = Path.Combine(AppConfiguration.DirectoryPath, "Logs");
        Directory.CreateDirectory(directory);
        RetainRecentLogs();
    }

    public void Information(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Failure(string message) => Write("ERROR", message);

    public void Failure(string message, Exception exception) => Write("ERROR", $"{message} {exception}");

    private void Write(string level, string message)
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            var line = $"{DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture)} [{level}] {message}";
            var path = Path.Combine(directory, $"{DateTime.Now:yyyy-MM-dd}.log");
            try
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch (IOException)
            {
                // A logging failure must not take down the sleep guard.
            }

            if (console)
            {
                Console.WriteLine(line);
            }
        }
    }

    private void RetainRecentLogs()
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.log"))
            {
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-7))
                {
                    File.Delete(file);
                }
            }
        }
        catch (IOException)
        {
            // Retention is best effort.
        }
        catch (UnauthorizedAccessException)
        {
            // Retention is best effort.
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            disposed = true;
        }
    }
}
