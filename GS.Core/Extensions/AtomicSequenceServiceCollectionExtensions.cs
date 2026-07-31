using GS.Core.Configuration;
using GS.Core.Sequencing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GS.Core.Extensions;

public static class AtomicSequenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAtomicSequenceGenerator"/> using Redis <c>INCR</c>
    /// (config section <see cref="AtomicSequenceOptions.SectionName"/>).
    /// </summary>
    public static IServiceCollection AddAtomicSequence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AtomicSequenceOptions>(
            configuration.GetSection(AtomicSequenceOptions.SectionName));
        services.TryAddSingleton<IAtomicSequenceGenerator, RedisAtomicSequenceGenerator>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IAtomicSequenceGenerator"/> using Redis <c>INCR</c>.
    /// </summary>
    public static IServiceCollection AddAtomicSequence(
        this IServiceCollection services,
        Action<AtomicSequenceOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IAtomicSequenceGenerator, RedisAtomicSequenceGenerator>();
        return services;
    }
}
