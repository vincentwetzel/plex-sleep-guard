namespace PlexSleepGuard.Power;

public interface IPowerManager
{
    IDisposable AcquireSystemRequired(string reason);
}

public interface IPowerRequestDiagnostics
{
    string Capture();
}
