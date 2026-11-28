namespace RepyPharma.Services.Abstractions;

/// <summary>Estado scoped por circuito. O lease impede operações visuais concorrentes e garante liberação.</summary>
public sealed class LoadingOperationState
{
    private readonly object _sync = new();
    private Guid? _current;
    private string _message = "";
    public event Action? Changed;
    public bool IsProcessing { get { lock (_sync) return _current.HasValue; } }
    public string Message { get { lock (_sync) return _message; } }
    public Operation? TryBegin(string message)
    {
        Guid id;
        lock (_sync)
        {
            if (_current.HasValue) return null;
            id = Guid.NewGuid(); _current = id; _message = message;
        }
        try { Changed?.Invoke(); }
        catch { lock (_sync) { _current = null; _message = ""; } throw; }
        return new(this, id);
    }
    private void Report(Guid id, string message)
    {
        lock (_sync) { if (_current != id) return; _message = message; }
        Changed?.Invoke();
    }
    private void End(Guid id)
    {
        lock (_sync) { if (_current != id) return; _current = null; _message = ""; }
        Changed?.Invoke();
    }
    public sealed class Operation : IDisposable
    {
        private LoadingOperationState? _owner;
        private readonly Guid _id;
        internal Operation(LoadingOperationState owner, Guid id) { _owner = owner; _id = id; }
        public void Report(string message) => _owner?.Report(_id, message);
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.End(_id);
    }
}
