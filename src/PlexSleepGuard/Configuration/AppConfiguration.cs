using System.Text.Json;

namespace PlexSleepGuard.Configuration;

public sealed class AppConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public string PlexServerUrl { get; set; } = "http://127.0.0.1:32400";
    public string PlexToken { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 5;
    public int GracePeriodMinutes { get; set; } = 15;

    public static string DirectoryPath => Path.Combine(
        Environment.GetEnvironmentVariable("PLEX_SLEEP_GUARD_DATA_DIR") ??
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlexSleepGuard");

    public static string FilePath => Path.Combine(DirectoryPath, "config.json");

    public static AppConfiguration Load(ILog log)
    {
        Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(FilePath))
        {
            var template = new AppConfiguration();
            template.Save();
            log.Warning($"Created configuration template at {FilePath}. PlexToken is missing; add it before relying on Plex detection.");
            return template;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var configuration = JsonSerializer.Deserialize<AppConfiguration>(json) ?? new AppConfiguration();
            configuration.Normalize();
            log.Information($"Configuration loaded from {FilePath} (server: {configuration.PlexServerUrl}, poll interval: {configuration.PollIntervalSeconds}s, grace period: {configuration.GracePeriodMinutes}m).");
            if (string.IsNullOrWhiteSpace(configuration.PlexToken))
            {
                log.Warning("PlexToken is missing from configuration. Add the token to enable authenticated Plex polling.");
            }

            return configuration;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            log.Failure($"Could not read {FilePath}; using safe defaults. {exception.Message}");
            var fallback = new AppConfiguration();
            fallback.Save();
            return fallback;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }

    public void Normalize()
    {
        if (!Uri.TryCreate(PlexServerUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            PlexServerUrl = "http://127.0.0.1:32400";
        }

        PlexServerUrl = PlexServerUrl.TrimEnd('/');
        PollIntervalSeconds = Math.Clamp(PollIntervalSeconds, 1, 3600);
        GracePeriodMinutes = Math.Clamp(GracePeriodMinutes, 0, 24 * 60);
    }
}
