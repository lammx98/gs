using GS.Core.Configuration;
using GS.Core.Ids;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GS.Core.Extensions;

public static class SnowflakeServiceCollectionExtensions
{
    /// <summary>Registers a singleton thread-safe <see cref="ISnowflakeIdGenerator"/>.</summary>
    public static IServiceCollection AddSnowflakeIdGenerator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SnowflakeOptions>(configuration.GetSection(SnowflakeOptions.SectionName));
        services.TryAddSingleton<ISnowflakeIdGenerator>(sp =>
        {
            var options = configuration.GetSection(SnowflakeOptions.SectionName).Get<SnowflakeOptions>()
                ?? new SnowflakeOptions();
            return new SnowflakeIdGenerator(options);
        });

        return services;
    }
}
