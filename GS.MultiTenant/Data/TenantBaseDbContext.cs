using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using GS.Core.Data;
using GS.Core.Ids;
using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Configuration;
using GS.MultiTenant.Extensions;
using GS.MultiTenant.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace GS.MultiTenant.Data;

/// <summary>
/// Base DbContext with hybrid database routing and shared-database isolation.
/// </summary>
public abstract class TenantBaseDbContext : MultiTenantDbContext
{
    private const string TenantQueryFilterName = "GS.MultiTenant.Tenant";
    private readonly IConnectionStringResolver _connectionStringResolver;
    private readonly ISnowflakeIdGenerator? _snowflakeIdGenerator;

    protected TenantBaseDbContext(
        IMultiTenantContextAccessor multiTenantContextAccessor,
        IConnectionStringResolver connectionStringResolver,
        DbContextOptions optionsBuilder,
        ISnowflakeIdGenerator? snowflakeIdGenerator = null)
        : base(multiTenantContextAccessor, optionsBuilder)
    {
        _connectionStringResolver = connectionStringResolver;
        _snowflakeIdGenerator = snowflakeIdGenerator;
    }

    /// <summary>Runs before Snowflake ids and other save pipeline steps.</summary>
    protected virtual void ApplyBeforeSaveChanges()
    {
        this.ApplySnowflakeIds(_snowflakeIdGenerator);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyBeforeSaveChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyBeforeSaveChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var tenant = TenantInfo as TenantModel;
        var connectionString = tenant is not null && _connectionStringResolver.UsesDedicatedDatabase(tenant)
            ? _connectionStringResolver.ResolveDedicated(tenant)
            : _connectionStringResolver.ResolveShared();

        ConfigureProvider(optionsBuilder, connectionString);
    }

    protected abstract void ConfigureProvider(DbContextOptionsBuilder optionsBuilder, string connectionString);

    /// <summary>
    /// The internal identifier used by tenant query filters. A missing tenant remains
    /// <see langword="null"/> so filters deny access rather than accidentally exposing shared data.
    /// </summary>
    public string? CurrentTenantId
    {
        get
        {
            var tenant = TenantInfo as TenantModel;

            return !string.IsNullOrWhiteSpace(tenant?.Id)
                ? tenant.Id
                : !string.IsNullOrWhiteSpace(tenant?.Identifier)
                    ? tenant.Identifier
                    : null;
        }
    }

    /// <summary>Whether the current context is routed to a dedicated tenant database.</summary>
    public bool UsesDedicatedTenantDatabase => (TenantInfo as TenantModel)?.UsesDedicatedDatabase == true;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is not null
                || entityType.IsOwned()
                || !typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType)
                || typeof(ITenantSharedEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(TenantBaseDbContext)
                .GetMethod(nameof(ConfigureTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [modelBuilder]);
        }

        modelBuilder.ApplyTenantSharedEntityQueryFilters(this);
    }

    private void ConfigureTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(TenantQueryFilterName, CreateTenantFilter<TEntity>());
    }

    private Expression<Func<TEntity, bool>> CreateTenantFilter<TEntity>()
        where TEntity : class, ITenantEntity
    {
        return entity =>
            UsesDedicatedTenantDatabase
            || entity.TenantId == CurrentTenantId;
    }
}
