using Finbuckle.MultiTenant.Abstractions;
using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Data;
using GS.MultiTenant.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GS.MultiTenant.Extensions;

public static class MultiTenantDbContextExtensions
{
    public static IServiceCollection AddTenantDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder, string>? configureProvider = null)
        where TContext : TenantBaseDbContext
    {
        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            if (configureProvider is null)
            {
                return;
            }

            var resolver = serviceProvider.GetRequiredService<IConnectionStringResolver>();
            var accessor = serviceProvider.GetRequiredService<IMultiTenantContextAccessor>();
            var tenant = accessor.MultiTenantContext?.TenantInfo as TenantModel;

            var connectionString = tenant is not null && resolver.UsesDedicatedDatabase(tenant)
                ? resolver.ResolveDedicated(tenant)
                : resolver.ResolveShared();

            configureProvider(options, connectionString);
        });

        return services;
    }
}
