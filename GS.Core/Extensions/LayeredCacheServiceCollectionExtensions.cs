using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GS.Core.Extensions;

public static class LayeredCacheServiceCollectionExtensions
{
    public static IServiceCollection AddLayeredCache(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<Caching.LayeredCacheOptions>(
                configuration.GetSection(Caching.LayeredCacheOptions.SectionName));
        }

        services.AddMemoryCache();
        services.AddSingleton<Caching.ILayeredCache, Caching.LayeredCache>();

        return services;
    }
}
