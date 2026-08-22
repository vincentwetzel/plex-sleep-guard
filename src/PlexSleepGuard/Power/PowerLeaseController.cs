using PlexSleepGuard.State;

namespace PlexSleepGuard.Power;

public sealed class PowerLeaseController : IDisposable
{
    private const string RequestReason = "PlexSleepGuard Plex playback and post-playback grace period";

    private readonly IPowerManager manager;
    private readonly ILog log;
    private IDisposable? lease;

    public PowerLeaseController(IPowerManager manager, ILog log)
    {
        this.manager = manager;
        this.log = log;
    }

    public void Acquire()
    {
        if (lease is not null)
        {
            return;
        }

        lease = manager.AcquireSystemRequired(RequestReason);
        log.Information("Windows power request created and set (system required only; display is not inhibited).");
    }

    public void Apply(PlaybackState state)
    {
        if (state is PlaybackState.Playing or PlaybackState.GracePeriod)
        {
            Acquire();
        }
        else
        {
            Release();
        }
    }

    public void Release()
    {
        var current = Interlocked.Exchange(ref lease, null);
        if (current is null)
        {
            return;
        }

        current.Dispose();
        log.Information("Windows power request cleared.");
    }

    public void Dispose() => Release();
}
