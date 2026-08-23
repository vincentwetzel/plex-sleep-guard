using System.Diagnostics;
using System.Text;

namespace PlexSleepGuard.Power;

public sealed class WindowsPowerRequestDiagnostics : IPowerRequestDiagnostics
{
    public string Capture()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add("/requests");

        try
        {
            if (!process.Start())
            {
                return "powercfg could not be started.";
            }

            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch (InvalidOperationException) { }
                return "powercfg /requests timed out.";
            }

            var text = output.GetAwaiter().GetResult();
            var errorText = error.GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                text = $"{text}\nSTDERR: {errorText}";
            }

            return Normalize(text);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return $"powercfg failed: {exception.Message}";
        }
    }

    private static string Normalize(string text)
    {
        var builder = new StringBuilder();
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (builder.Length > 0)
            {
                builder.Append(" | ");
            }

            builder.Append(line);
        }

        return builder.Length == 0 ? "powercfg /requests returned no output." : builder.ToString();
    }
}
