namespace GS.Core.Caching;

/// <summary>
/// Lookup order when reading from cache.
/// </summary>
public enum CacheLookupStrategy
{
    /// <summary>Memory first, then Redis when memory misses.</summary>
    MemoryThenRedis,

    /// <summary>Redis only.</summary>
    RedisOnly
}
