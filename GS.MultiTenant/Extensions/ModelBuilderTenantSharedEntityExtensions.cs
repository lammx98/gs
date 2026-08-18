using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace GS.MultiTenant.Extensions;

/// <summary>
/// Configures tenant-aware access to entities whose records may be shared by all tenants.
/// </summary>
public static class ModelBuilderTenantSharedEntityExtensions
{
    /// <summary>The name used for the shared-tenant global query filter.</summary>
    public const string TenantSharedQueryFilterName = "GS.MultiTenant.SharedTenant";

    /// <summary>
    /// Applies a global query filter to every root entity implementing
    /// <see cref="ITenantSharedEntity"/>. The filter exposes only records for the current tenant
    /// and shared records (where <c>TenantId</c> is <see langword="null"/>).
    /// </summary>
    /// <remarks>
    /// The filter is deliberately fail-closed: no records are returned when the context has no
    /// resolved tenant. Call this after all entity types have been added to the model.
    /// </remarks>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="dbContext">The context whose tenant is evaluated when a query executes.</param>
    /// <returns>The supplied <paramref name="modelBuilder"/> for chaining.</returns>
    public static ModelBuilder ApplyTenantSharedEntityQueryFilters(
        this ModelBuilder modelBuilder,
        TenantBaseDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(dbContext);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null
                || entityType.BaseType is not null
                || entityType.IsOwned()
                || !typeof(ITenantSharedEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            ConfigureSharedTenantEntityMethod
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(null, [modelBuilder, dbContext]);
        }

        return modelBuilder;
    }

    private static readonly MethodInfo ConfigureSharedTenantEntityMethod = typeof(ModelBuilderTenantSharedEntityExtensions)
        .GetMethod(nameof(ConfigureSharedTenantEntity), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void ConfigureSharedTenantEntity<TEntity>(ModelBuilder modelBuilder, TenantBaseDbContext dbContext)
        where TEntity : class, ITenantSharedEntity
    {
        modelBuilder.Entity<TEntity>(builder =>
        {
            // An implementation may declare TenantId as non-nullable even though this contract
            // permits shared records. Configure the database column explicitly as nullable.
            builder.Property(entity => entity.TenantId).IsRequired(false);
            builder.HasQueryFilter(TenantSharedQueryFilterName, CreateTenantSharedEntityFilter<TEntity>(dbContext));
        });
    }

    private static Expression<Func<TEntity, bool>> CreateTenantSharedEntityFilter<TEntity>(TenantBaseDbContext dbContext)
        where TEntity : class, ITenantSharedEntity
    {
        return entity =>
            dbContext.CurrentTenantId != null
            && (entity.TenantId == dbContext.CurrentTenantId || entity.TenantId == null);
    }
}
