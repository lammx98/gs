namespace GS.Core.Caching;

public interface ILayeredCache
{
    Task<T?> GetAsync<T>(
        string key,
        CacheLookupStrategy strategy,
        CancellationToken cancellationToken = default);

    Task<T?> GetAsync<T>(
        string key,
        CacheLookupStrategy strategy,
        Func<CancellationToken, Task<T?>> fallback,
        CacheStorageTarget storeFallbackResult = CacheStorageTarget.MemoryAndRedis,
        CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        CacheStorageTarget target,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        string key,
        CacheStorageTarget target = CacheStorageTarget.MemoryAndRedis,
        CancellationToken cancellationToken = default);
}
