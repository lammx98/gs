using Finbuckle.MultiTenant.Abstractions;
using GS.Core.Auth;
using GS.Core.Data;
using GS.Core.Ids;
using GS.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GS.MultiTenant.Data;

/// <summary>
/// <see cref="TenantBaseDbContext"/> that automatically applies <see cref="DbContextAuditExtensions.ApplyAutomaticAuditFields"/> on save.
/// For contexts that should not auto-audit, use <see cref="TenantBaseDbContext"/> and optionally call the extension manually.
/// </summary>
public abstract class AuditableTenantBaseDbContext : TenantBaseDbContext
{
    private readonly ICurrentUserAccessor? _currentUserAccessor;

    protected AuditableTenantBaseDbContext(
        IMultiTenantContextAccessor multiTenantContextAccessor,
        IConnectionStringResolver connectionStringResolver,
        DbContextOptions options,
        ISnowflakeIdGenerator? snowflakeIdGenerator = null,
        ICurrentUserAccessor? currentUserAccessor = null)
        : base(multiTenantContextAccessor, connectionStringResolver, options, snowflakeIdGenerator)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    /// <summary>User context used for actor audit fields; may be null (e.g. design-time, background jobs).</summary>
    protected ICurrentUserAccessor? AuditUserContext => _currentUserAccessor;

    /// <summary>Runs after Snowflake ids and before audit fields are applied.</summary>
    protected virtual void OnBeforeSaveChanges()
    {
    }

    protected override void ApplyBeforeSaveChanges()
    {
        OnBeforeSaveChanges();
        base.ApplyBeforeSaveChanges();
        this.ApplyAutomaticAuditFields(_currentUserAccessor);
    }
}
