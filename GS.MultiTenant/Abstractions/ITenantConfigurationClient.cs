using GS.MultiTenant.Models;

namespace GS.MultiTenant.Abstractions;

public interface ITenantConfigurationClient
{
    Task<TenantModel?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    Task<TenantModel?> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default);
}
