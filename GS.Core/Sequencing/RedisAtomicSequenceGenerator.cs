using System.Globalization;
using GS.Core.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GS.Core.Sequencing;

/// <summary>
/// Redis atomic counter using <c>INCR</c>. Sets key TTL on first increment via a Lua script
/// so create + expire stay atomic.
/// </summary>
public sealed class RedisAtomicSequenceGenerator : IAtomicSequenceGenerator, IDisposable
{
    private const string IncrWithExpireScript = """
        local value = redis.call('INCR', KEYS[1])
        if value == 1 then
          redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        return value
        """;

    private readonly AtomicSequenceOptions _options;
    private readonly Lazy<IConnectionMultiplexer> _connection;
    private bool _disposed;

    public RedisAtomicSequenceGenerator(IOptions<AtomicSequenceOptions> options)
    {
        _options = options.Value;
        _connection = new Lazy<IConnectionMultiplexer>(() =>
            ConnectionMultiplexer.Connect(_options.RedisConnectionString));
    }

    public Task<long> GetNextDailyAsync(
        DateOnly date,
        string? scope = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetNextAsync(BuildDailyKey(date, scope), cancellationToken);
    }

    public async Task<long> GetNextAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        var db = _connection.Value.GetDatabase();
        var ttlMs = (long)_options.KeyTtl.TotalMilliseconds;
        if (ttlMs <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(AtomicSequenceOptions.KeyTtl)} must be greater than zero.");
        }

        var result = await db.ScriptEvaluateAsync(
                IncrWithExpireScript,
                keys: [key],
                values: [ttlMs])
            .ConfigureAwait(false);

        return (long)result;
    }

    internal string BuildDailyKey(DateOnly date, string? scope)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.KeyPrefix) ? "seq" : _options.KeyPrefix.Trim();
        var datePart = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(scope))
        {
            return $"{prefix}:{datePart}";
        }

        return $"{prefix}:{scope.Trim()}:{datePart}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }

        _disposed = true;
    }
}
