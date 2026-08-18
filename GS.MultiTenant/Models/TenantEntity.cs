using GS.Core.Ids;
using GS.MultiTenant.Abstractions;

namespace GS.MultiTenant.Models;

public abstract class TenantEntity : ITenantEntity, IHasLongId
{
    public long Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
}