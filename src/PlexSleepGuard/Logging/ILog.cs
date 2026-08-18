namespace PlexSleepGuard;

public interface ILog : IDisposable
{
    void Information(string message);
    void Warning(string message);
    void Failure(string message);
    void Failure(string message, Exception exception);
}
