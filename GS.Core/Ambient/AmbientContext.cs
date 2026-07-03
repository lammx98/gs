namespace GS.Core.Ambient;

/// <summary>
/// Async-local ambient value with restore-on-dispose semantics.
/// </summary>
public sealed class AmbientContext<T>
{
    private readonly AsyncLocal<T?> _current = new();

    public T? Value => _current.Value;

    public IDisposable Set(T? value)
    {
        var previous = _current.Value;
        _current.Value = value;
        return new Scope(this, previous);
    }

    private sealed class Scope(AmbientContext<T> context, T? previous) : IDisposable
    {
        public void Dispose() => context._current.Value = previous;
    }
}
