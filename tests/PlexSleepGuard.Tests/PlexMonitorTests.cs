using System.Net;
using System.Xml.Linq;
using PlexSleepGuard.Configuration;
using PlexSleepGuard.Plex;

namespace PlexSleepGuard.Tests;

public sealed class PlexMonitorTests
{
    [Fact]
    public void ParsesPlexMediaContainerSessionsAndTreatsPausedAsActive()
    {
        var document = XDocument.Parse("""
            <MediaContainer size="3">
              <Video type="movie" title="Movie" state="playing" sessionKey="1" />
              <Video type="episode" grandparentTitle="Show" title="Episode" state="paused" sessionKey="2" />
              <Video type="movie" title="Stopped" state="stopped" sessionKey="3" />
            </MediaContainer>
            """);
        var sessions = PlexMonitor.ParseSessions(document);
        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, session => session.State == "playing");
        Assert.Contains(sessions, session => session.State == "paused");
        Assert.DoesNotContain(sessions, session => session.State == "stopped");
    }

    [Fact]
    public async Task SuccessfulPollDoesNotLogEveryPollingCycle()
    {
        using var client = new HttpClient(new StaticResponseHandler("<MediaContainer size=\"0\" />"));
        using var log = new TestLog();
        using var monitor = new PlexMonitor(new AppConfiguration(), log, client);

        var result = await monitor.PollAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(log.InformationMessages);
    }

    private sealed class StaticResponseHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
    }

    private sealed class TestLog : ILog
    {
        public List<string> InformationMessages { get; } = [];

        public void Information(string message) => InformationMessages.Add(message);
        public void Warning(string message) { }
        public void Failure(string message) { }
        public void Failure(string message, Exception exception) { }
        public void Dispose() { }
    }
}
