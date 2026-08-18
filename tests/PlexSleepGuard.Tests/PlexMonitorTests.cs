using System.Xml.Linq;
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
}
