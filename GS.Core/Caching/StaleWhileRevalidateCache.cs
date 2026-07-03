using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GS.Core.Caching;

internal sealed class StaleWhileRevalidateEntry<T>
{
    public required T Value { get; init; }

    public DateTimeOffset CachedAt { get; init; }

    public bool IsStale(TimeSpan staleThreshold) =>
        DateTimeOffset.UtcNow - CachedAt >= staleThreshold;
}

public sealed class StaleWhileRevalidateCache<T>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly StaleWhileRevalidateCacheOptions _options;
    private readonly ILogger<StaleWhileRevalidateCache<T>> _logger;

    public StaleWhileRevalidateCache(
        IMemoryCache memoryCache,
        IOptions<StaleWhileRevalidateCacheOptions> options,
        ILogger<StaleWhileRevalidateCache<T>> logger,
        IDistributedCache? distributedCache = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        Func<T, CancellationToken, Task>? onRefreshFailed = null,
        CancellationToken cancellationToken = default)
    {
        if (TryGetMemoryEntry(key, out var entry) && entry is not null)
        {
            TriggerRefreshIfStale(key, factory, onRefreshFailed, entry);
            return entry.Value;
        }

        if (_distributedCache is not null)
        {
            var distributed = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrWhiteSpace(distributed))
            {
                entry = JsonSerializer.Deserialize<StaleWhileRevalidateEntry<T>>(distributed, JsonOptions);
                if (entry is not null)
                {
                    _memoryCache.Set(key, entry, _options.AbsoluteExpiration);
                    TriggerRefreshIfStale(key, factory, onRefreshFailed, entry);
                    return entry.Value;
                }
            }
        }

        var value = await factory(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, cancellationToken);
        }

        return value;
    }

    public Task SetAsync(string key, T value, CancellationToken cancellationToken = default)
    {
        var entry = new StaleWhileRevalidateEntry<T>
        {
            Value = value,
            CachedAt = DateTimeOffset.UtcNow
        };

        _memoryCache.Set(key, entry, _options.AbsoluteExpiration);

        if (_distributedCache is null)
        {
            return Task.CompletedTask;
        }

        var payload = JsonSerializer.Serialize(entry, JsonOptions);
        return _distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.AbsoluteExpiration
            },
            cancellationToken);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        _distributedCache?.Remove(key);
    }

    private bool TryGetMemoryEntry(string key, out StaleWhileRevalidateEntry<T>? entry)
    {
        if (_memoryCache.TryGetValue(key, out StaleWhileRevalidateEntry<T>? cached) && cached is not null)
        {
            entry = cached;
            return true;
        }

        entry = null;
        return false;
    }

    private void TriggerRefreshIfStale(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        Func<T, CancellationToken, Task>? onRefreshFailed,
        StaleWhileRevalidateEntry<T> entry)
    {
        if (!entry.IsStale(_options.StaleThreshold))
        {
            return;
        }

        _ = RefreshInBackgroundAsync(key, factory, onRefreshFailed);
    }

    private async Task RefreshInBackgroundAsync(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        Func<T, CancellationToken, Task>? onRefreshFailed)
    {
        try
        {
            var value = await factory(CancellationToken.None);
            if (value is not null)
            {
                await SetAsync(key, value, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stale-while-revalidate refresh failed for cache key {CacheKey}", key);
            if (onRefreshFailed is not null && TryGetMemoryEntry(key, out var entry) && entry is not null)
            {
                await onRefreshFailed(entry.Value, CancellationToken.None);
            }
        }
    }
}
