using GS.MultiTenant.Models;

namespace GS.MultiTenant.Abstractions;

public interface ITenantConfigurationClient
{
    Task<TenantModel?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}
