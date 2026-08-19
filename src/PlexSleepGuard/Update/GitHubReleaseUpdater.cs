using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlexSleepGuard.Update;

internal static class GitHubReleaseUpdater
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/vincentwetzel/plex-sleep-guard/releases/latest";
    private const string SetupAssetName = "PlexSleepGuard-Setup.exe";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static async Task<bool> TryStartUpdateAsync(string installedExecutablePath, CancellationToken cancellationToken = default)
    {
        string? downloadedPath = null;
        var handedOff = false;

        try
        {
            var currentVersion = typeof(Program).Assembly.GetName().Version ?? new Version(0, 0);
            using var releaseResponse = await HttpClient.GetAsync(
                LatestReleaseUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (!releaseResponse.IsSuccessStatusCode)
            {
                return false;
            }

            await using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(releaseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (release is null || !TryParseVersion(release.TagName, out var latestVersion) || latestVersion <= currentVersion)
            {
                return false;
            }

            var asset = release.Assets?.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, SetupAssetName, StringComparison.OrdinalIgnoreCase));
            if (asset is null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl) ||
                string.IsNullOrWhiteSpace(asset.Digest) ||
                !asset.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            downloadedPath = Path.Combine(Path.GetTempPath(), $"PlexSleepGuard-Setup-{Guid.NewGuid():N}.exe");
            using var assetResponse = await HttpClient.GetAsync(
                asset.BrowserDownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (!assetResponse.IsSuccessStatusCode)
            {
                return false;
            }

            await using (var output = File.Create(downloadedPath))
            await assetResponse.Content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);

            var expectedDigest = asset.Digest["sha256:".Length..];
            await using var downloadedFile = File.OpenRead(downloadedPath);
            var actualDigest = Convert.ToHexString(await SHA256.HashDataAsync(downloadedFile, cancellationToken).ConfigureAwait(false));
            if (!string.Equals(expectedDigest, actualDigest, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            handedOff = UpdateApplier.Start(
                downloadedPath,
                installedExecutablePath,
                Environment.ProcessId);
            return handedOff;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            if (!handedOff && downloadedPath is not null)
            {
                TryDelete(downloadedPath);
            }
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PlexSleepGuard", "0.1.7"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static bool TryParseVersion(string? tag, out Version version)
    {
        version = new Version();
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var value = tag.Trim();
        if (value.StartsWith('v') || value.StartsWith('V'))
        {
            value = value[1..];
        }

        if (Version.TryParse(value, out var parsedVersion))
        {
            version = parsedVersion;
            return true;
        }

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
            // Windows may briefly keep a temporary update file locked.
        }
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("digest")]
        public string? Digest { get; set; }
    }
}
