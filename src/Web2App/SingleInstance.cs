namespace Web2App;

/// <summary>
/// One running instance per app file: a second launch brings the existing window to the front and exits.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex;
    private readonly EventWaitHandle activation;
    private readonly RegisteredWaitHandle registration;

    private SingleInstance(Mutex mutex, EventWaitHandle activation)
    {
        this.mutex = mutex;
        this.activation = activation;
        registration = ThreadPool.RegisterWaitForSingleObject(
            activation, (_, _) => ActivationRequested?.Invoke(), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    /// <summary>Raised on a thread-pool thread when another launch of the same app asks to be shown.</summary>
    public event Action? ActivationRequested;

    /// <summary>Returns the instance owner, or null after signalling the instance that is already running.</summary>
    public static SingleInstance? Acquire(string appId)
    {
        var mutex = new Mutex(initiallyOwned: true, $@"Local\{appId}", out var createdNew);
        var activation = new EventWaitHandle(false, EventResetMode.AutoReset, $@"Local\{appId}.activate");

        if (createdNew)
        {
            return new SingleInstance(mutex, activation);
        }

        NativeMethods.AllowAnyForegroundWindow();
        activation.Set();
        activation.Dispose();
        mutex.Dispose();
        return null;
    }

    public void Dispose()
    {
        registration.Unregister(null);
        activation.Dispose();
        mutex.ReleaseMutex();
        mutex.Dispose();
    }
}
