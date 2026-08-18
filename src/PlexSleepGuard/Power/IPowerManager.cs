namespace PlexSleepGuard.Power;

public interface IPowerManager
{
    IDisposable AcquireSystemRequired(string reason);
}
