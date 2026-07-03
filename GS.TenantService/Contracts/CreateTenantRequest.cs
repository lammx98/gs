using GS.MultiTenant.Models;
using System.ComponentModel.DataAnnotations;

namespace GS.TenantService.Contracts;

public sealed class CreateTenantRequest
{
    [Required]
    [MaxLength(64)]
    public string TenantCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string TenantName { get; set; } = string.Empty;

    public TenantTier Tier { get; set; } = TenantTier.Basic;

    [MaxLength(2048)]
    public string? ConnectionString { get; set; }
}
