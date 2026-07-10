using GS.Core.Audit;
using GS.Core.Ids;
using GS.MultiTenant.Abstractions;

namespace GS.MultiTenant.Models;

/// <summary>Tenant-scoped entity with automatic timestamp audit fields.</summary>
public abstract class AuditableTenantEntity : IAuditableEntity, ITenantEntity, IHasLongId
{
    public long Id { get; set; }

    public string TenantId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Tenant-scoped entity with timestamp and actor audit fields.</summary>
public abstract class AuditableTenantEntityWithUser : AuditableTenantEntity, IAuditableEntityWithUser
{
    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
}
