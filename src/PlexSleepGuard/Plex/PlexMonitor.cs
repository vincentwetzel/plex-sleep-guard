using System.Net.Http.Headers;
using System.Xml.Linq;
using PlexSleepGuard.Configuration;

namespace PlexSleepGuard.Plex;

public sealed class PlexMonitor : IDisposable
{
    private readonly AppConfiguration configuration;
    private readonly ILog log;
    private readonly HttpClient client;
    private readonly bool ownsClient;

    public PlexMonitor(AppConfiguration configuration, ILog log, HttpClient? client = null)
    {
        this.configuration = configuration;
        this.log = log;
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        ownsClient = client is null;
    }

    public async Task<PlexPollResult> PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{configuration.PlexServerUrl}/status/sessions");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            if (!string.IsNullOrWhiteSpace(configuration.PlexToken))
            {
                request.Headers.Add("X-Plex-Token", configuration.PlexToken);
            }

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var error = $"Plex returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
                log.Failure(error);
                return PlexPollResult.Failed(error);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            var sessions = ParseSessions(document);
            log.Information($"Plex reachable; {sessions.Count} relevant playback session(s) detected.");
            return PlexPollResult.Succeeded(sessions);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            log.Failure($"Plex API/network error while polling {configuration.PlexServerUrl}/status/sessions: {exception.Message}");
            return PlexPollResult.Failed(exception.Message);
        }
    }

    public static IReadOnlyList<PlexSession> ParseSessions(XDocument document)
    {
        var root = document.Root;
        if (root is null)
        {
            return Array.Empty<PlexSession>();
        }

        var sessions = new List<PlexSession>();
        foreach (var element in root.Elements())
        {
            var type = (string?)element.Attribute("type") ?? string.Empty;
            var state = (string?)element.Attribute("state") ?? "unknown";
            var key = (string?)element.Attribute("sessionKey") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(type) || (string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(state)))
            {
                continue;
            }

            // /status/sessions exposes Video/Track (and sometimes other media types) as direct children.
            // A stopped child is retained for status visibility but is not considered active by PlexSession.
            var title = (string?)element.Attribute("grandparentTitle") ??
                        (string?)element.Attribute("parentTitle") ??
                        (string?)element.Attribute("title") ?? "(untitled)";
            sessions.Add(new PlexSession(title, type, state, key));
        }

        return sessions.Where(static session => session.IsActive).ToArray();
    }

    public void Dispose()
    {
        if (ownsClient)
        {
            client.Dispose();
        }
    }
}
