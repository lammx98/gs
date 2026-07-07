namespace GS.Core.Caching;

public sealed class LayeredCacheOptions
{
    public const string SectionName = "LayeredCache";

    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
}
