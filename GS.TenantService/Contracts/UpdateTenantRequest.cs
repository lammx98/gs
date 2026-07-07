using GS.MultiTenant.Models;
using System.ComponentModel.DataAnnotations;

namespace GS.TenantService.Contracts;

public sealed class UpdateTenantRequest
{
    [Required]
    [MaxLength(256)]
    public string TenantName { get; set; } = string.Empty;

    public TenantTier Tier { get; set; } = TenantTier.Basic;

    public bool UsesDedicatedDatabase { get; set; }

    [MaxLength(256)]
    public string? DatabaseHost { get; set; }

    public int? DatabasePort { get; set; }

    [MaxLength(128)]
    public string? CredentialsRef { get; set; }

    public bool IsActive { get; set; } = true;
}
