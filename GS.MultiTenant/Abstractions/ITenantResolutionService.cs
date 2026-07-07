using GS.MultiTenant.Models;

namespace GS.MultiTenant.Abstractions;

public interface ITenantResolutionService
{
    Task<TenantModel?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    Task<TenantModel?> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default);

    Task SetAsync(TenantModel tenant, CancellationToken cancellationToken = default);

    Task ClearAsync(string tenantCode, string? tenantId = null, CancellationToken cancellationToken = default);
}
