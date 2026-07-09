using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using GS.Core.Caching;
using GS.Core.Extensions;
using GS.MultiTenant.Grpc.Tenant;
using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Configuration;
using GS.MultiTenant.Exceptions;
using GS.MultiTenant.Http;
using GS.MultiTenant.Messaging;
using GS.MultiTenant.Models;
using GS.MultiTenant.Services;
using GS.MultiTenant.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GS.MultiTenant.Extensions;

public static class MultiTenantServiceCollectionExtensions
{
    public static IServiceCollection AddMultiTenantServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MultiTenantOptions>? configure = null)
    {
        services.Configure<MultiTenantOptions>(configuration.GetSection(MultiTenantOptions.SectionName));
        if (configure is not null)
        {
            services.Configure(configure);
        }

        var options = configuration.GetSection(MultiTenantOptions.SectionName).Get<MultiTenantOptions>()
            ?? new MultiTenantOptions();
        configure?.Invoke(options);

        if (options.UseRedisCache && !string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            services.AddStackExchangeRedisCache(redis => redis.Configuration = options.RedisConnectionString);
        }

        services.Configure<LayeredCacheOptions>(cache =>
        {
            cache.DefaultExpiration = options.CacheAbsoluteExpiration;
        });

        services.AddGsLayeredCache();
        services.TryAddSingleton<ITenantBypassService, TenantBypassService>();
        services.TryAddScoped<ICurrentTenantAccessor, CurrentTenantAccessor>();
        services.TryAddSingleton<IConnectionStringResolver, PostgreSqlConnectionStringResolver>();
        services.TryAddSingleton<ITenantResolutionService, TenantResolutionService>();

        if (!string.IsNullOrWhiteSpace(options.TenantServiceGrpcAddress))
        {
            services.AddGsGrpcClient<TenantResolver.TenantResolverClient>(options.TenantServiceGrpcAddress);
        }

        services.TryAddSingleton<ITenantConfigurationClient, GrpcTenantConfigurationClient>();

        var requireTenant = options.RequireTenant;

        services.AddMultiTenant<TenantModel>(multiTenantOptions =>
            {
                multiTenantOptions.Events.OnTenantResolveCompleted = context =>
                {
                    if (!requireTenant || context.IsResolved)
                    {
                        return Task.CompletedTask;
                    }

                    throw new TenantNotResolvedException();
                };
            })
            .WithHeaderStrategy(options.TenantHeaderName)
            .WithHostStrategy(options.HostTemplate)
            .WithClaimStrategy(options.JwtTenantClaimType)
            .WithDelegateStrategy(async _ =>
            {
                return await Task.FromResult(TenantMessageContext.TenantId);
            })
            .WithStore<CachedTenantStore>(ServiceLifetime.Singleton);

        services.AddTransient<TenantPropagationDelegatingHandler>();

        return services;
    }

    public static IHttpClientBuilder AddTenantPropagation(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<TenantPropagationDelegatingHandler>();
    }

    /// <summary>
    /// Alias documented in DX01 samples.
    /// </summary>
    public static IServiceCollection AddClinicMultiTenant(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MultiTenantOptions>? configure = null) =>
        services.AddMultiTenantServices(configuration, configure);
}
