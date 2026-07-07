namespace GS.Core.Caching;

/// <summary>
/// Where to write or remove cache entries.
/// </summary>
public enum CacheStorageTarget
{
    Memory,
    Redis,
    MemoryAndRedis
}
