namespace GS.Core.Caching;

public sealed class StaleWhileRevalidateCacheOptions
{
    public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromMinutes(5);
}
