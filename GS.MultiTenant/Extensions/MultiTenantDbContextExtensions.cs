using GS.MultiTenant.Configuration;
using GS.MultiTenant.Data;
using GS.MultiTenant.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GS.MultiTenant.Extensions;

public static class MultiTenantDbContextExtensions
{
    public static IServiceCollection AddTenantDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configureProvider = null)
        where TContext : TenantBaseDbContext
    {
        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            var tenantOptions = serviceProvider.GetRequiredService<IOptions<MultiTenantOptions>>().Value;
            var accessor = serviceProvider.GetRequiredService<Finbuckle.MultiTenant.Abstractions.IMultiTenantContextAccessor>();
            var tenant = accessor.MultiTenantContext?.TenantInfo as TenantModel;

            var connectionString = tenant?.UsesDedicatedDatabase == true
                ? tenant.ConnectionString
                : tenantOptions.SharedDatabaseConnectionString;

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                configureProvider?.Invoke(options);
            }
        });

        return services;
    }
}
