namespace GS.Core.Configuration;

public sealed class AtomicSequenceOptions
{
    public const string SectionName = "AtomicSequence";

    /// <summary>Redis connection string, e.g. <c>localhost:6379</c>.</summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>Key prefix. Daily keys are <c>{KeyPrefix}:yyyyMMdd</c>.</summary>
    public string KeyPrefix { get; set; } = "seq";

    /// <summary>
    /// TTL applied when a sequence key is first created.
    /// Recommended 24–48 hours so keys auto-expire after the day rolls over.
    /// </summary>
    public TimeSpan KeyTtl { get; set; } = TimeSpan.FromHours(48);
}
