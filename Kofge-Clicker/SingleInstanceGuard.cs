namespace KofgeClicker;

internal sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = @"Local\Kofge-Clicker.SingleInstance";
    private readonly Mutex _mutex;
    private bool _ownsMutex;

    private SingleInstanceGuard(Mutex mutex)
    {
        _mutex = mutex;
        _ownsMutex = true;
    }

    internal static SingleInstanceGuard? TryAcquire()
    {
        try
        {
            var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                return null;
            }

            return new SingleInstanceGuard(mutex);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }

        _mutex.Dispose();
    }
}
