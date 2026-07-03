using GS.MultiTenant.Models;

namespace GS.TenantService.Data;

public class TenantEntity
{
    public Guid Id { get; set; }

    public string TenantCode { get; set; } = string.Empty;

    public string TenantName { get; set; } = string.Empty;

    public TenantTier Tier { get; set; } = TenantTier.Basic;

    public string? ConnectionString { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
