using System.Threading;

namespace PlexSleepGuard.Setup;

internal sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = @"Local\PlexSleepGuard.SingleInstance";
    private readonly Mutex mutex;

    private SingleInstanceGuard(Mutex mutex)
    {
        this.mutex = mutex;
    }

    public static SingleInstanceGuard? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
        {
            return new SingleInstanceGuard(mutex);
        }

        mutex.Dispose();
        return null;
    }

    public void Dispose()
    {
        try
        {
            mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // The mutex was already released or abandoned.
        }
        finally
        {
            mutex.Dispose();
        }
    }
}
