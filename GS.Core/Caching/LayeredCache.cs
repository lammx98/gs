using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GS.Core.Caching;

public sealed class LayeredCache : ILayeredCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly LayeredCacheOptions _options;

    public LayeredCache(
        IMemoryCache memoryCache,
        IOptions<LayeredCacheOptions> options,
        IDistributedCache? distributedCache = null)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
        _distributedCache = distributedCache;
    }

    public Task<T?> GetAsync<T>(
        string key,
        CacheLookupStrategy strategy,
        CancellationToken cancellationToken = default) =>
        GetAsync<T>(key, strategy, fallback: null, CacheStorageTarget.MemoryAndRedis, cancellationToken);

    public async Task<T?> GetAsync<T>(
        string key,
        CacheLookupStrategy strategy,
        Func<CancellationToken, Task<T?>>? fallback,
        CacheStorageTarget storeFallbackResult = CacheStorageTarget.MemoryAndRedis,
        CancellationToken cancellationToken = default)
    {
        var cached = strategy switch
        {
            CacheLookupStrategy.MemoryThenRedis => await GetMemoryThenRedisAsync<T>(key, cancellationToken),
            CacheLookupStrategy.RedisOnly => await GetRedisAsync<T>(key, cancellationToken),
            _ => default(T?)
        };

        if (cached is not null)
        {
            return cached;
        }

        if (fallback is null)
        {
            return default;
        }

        var value = await fallback(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, storeFallbackResult, cancellationToken);
        }

        return value;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheStorageTarget target,
        CancellationToken cancellationToken = default)
    {
        if (ShouldUseMemory(target))
        {
            _memoryCache.Set(key, value, _options.DefaultExpiration);
        }

        if (ShouldUseRedis(target))
        {
            await SetRedisAsync(key, value, cancellationToken);
        }
    }

    public async Task ClearAsync(
        string key,
        CacheStorageTarget target = CacheStorageTarget.MemoryAndRedis,
        CancellationToken cancellationToken = default)
    {
        if (ShouldUseMemory(target))
        {
            _memoryCache.Remove(key);
        }

        if (ShouldUseRedis(target) && _distributedCache is not null)
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
    }

    private async Task<T?> GetMemoryThenRedisAsync<T>(string key, CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(key, out T? memoryValue) && memoryValue is not null)
        {
            return memoryValue;
        }

        var redisValue = await GetRedisAsync<T>(key, cancellationToken);
        if (redisValue is not null)
        {
            _memoryCache.Set(key, redisValue, _options.DefaultExpiration);
        }

        return redisValue;
    }

    private async Task<T?> GetRedisAsync<T>(string key, CancellationToken cancellationToken)
    {
        if (_distributedCache is null)
        {
            return default;
        }

        var payload = await _distributedCache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(payload, JsonOptions);
    }

    private async Task SetRedisAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        if (_distributedCache is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(value, JsonOptions);
        await _distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.DefaultExpiration
            },
            cancellationToken);
    }

    private static bool ShouldUseMemory(CacheStorageTarget target) =>
        target is CacheStorageTarget.Memory or CacheStorageTarget.MemoryAndRedis;

    private static bool ShouldUseRedis(CacheStorageTarget target) =>
        target is CacheStorageTarget.Redis or CacheStorageTarget.MemoryAndRedis;
}
