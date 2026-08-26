using GS.MultiTenant.Models;

namespace GS.TenantService.Data;

public class TenantEntity
{
    public string Id { get; set; } = string.Empty;

    public string TenantCode { get; set; } = string.Empty;

    public string TenantName { get; set; } = string.Empty;

    public TenantTier Tier { get; set; } = TenantTier.Basic;

    public bool UsesDedicatedDatabase { get; set; }

    public string? DatabaseHost { get; set; }

    public int? DatabasePort { get; set; }

    public string? CredentialsRef { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
