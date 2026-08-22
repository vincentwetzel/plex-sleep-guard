using PlexSleepGuard.Power;
using PlexSleepGuard.State;

namespace PlexSleepGuard.Tests;

public sealed class PowerLeaseControllerTests
{
    [Fact]
    public void PowerLeaseRemainsActiveFromPlayingThroughGraceAndClearsAtIdle()
    {
        var powerManager = new TestPowerManager();
        using var log = new TestLog();
        using var controller = new PowerLeaseController(powerManager, log);

        controller.Apply(PlaybackState.Playing);
        Assert.Equal(1, powerManager.ActiveLeaseCount);
        Assert.Single(powerManager.Reasons);
        Assert.Contains("playback", powerManager.Reasons[0], StringComparison.OrdinalIgnoreCase);

        controller.Apply(PlaybackState.GracePeriod);
        Assert.Equal(1, powerManager.ActiveLeaseCount);

        controller.Apply(PlaybackState.Idle);
        Assert.Equal(0, powerManager.ActiveLeaseCount);
    }

    private sealed class TestPowerManager : IPowerManager
    {
        public int ActiveLeaseCount { get; private set; }
        public List<string> Reasons { get; } = [];

        public IDisposable AcquireSystemRequired(string reason)
        {
            Reasons.Add(reason);
            ActiveLeaseCount++;
            return new Lease(this);
        }

        private sealed class Lease(TestPowerManager manager) : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    manager.ActiveLeaseCount--;
                }
            }
        }
    }

    private sealed class TestLog : ILog
    {
        public void Information(string message) { }
        public void Warning(string message) { }
        public void Failure(string message) { }
        public void Failure(string message, Exception exception) { }
        public void Dispose() { }
    }
}
