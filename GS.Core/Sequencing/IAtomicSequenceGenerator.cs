namespace GS.Core.Sequencing;

/// <summary>Atomic sequence generator backed by Redis <c>INCR</c>.</summary>
public interface IAtomicSequenceGenerator
{
    /// <summary>
    /// Atomically increments the daily sequence key and returns the next value.
    /// Key format: <c>{prefix}:yyyyMMdd</c>, or <c>{prefix}:{scope}:yyyyMMdd</c> when <paramref name="scope"/> is set.
    /// TTL is set when the key is created (first <c>INCR</c> of the day).
    /// </summary>
    /// <param name="date">Calendar date for the sequence (UTC recommended).</param>
    /// <param name="scope">Optional scope (e.g. tenant id) to isolate counters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<long> GetNextDailyAsync(
        DateOnly date,
        string? scope = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments an arbitrary Redis key (absolute key name) and returns the next value.
    /// TTL is set when the key is created.
    /// </summary>
    Task<long> GetNextAsync(string key, CancellationToken cancellationToken = default);
}
