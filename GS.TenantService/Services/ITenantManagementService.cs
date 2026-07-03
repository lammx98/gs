using GS.MultiTenant.Models;
using GS.TenantService.Contracts;

namespace GS.TenantService.Services;

public interface ITenantManagementService
{
    Task<IReadOnlyList<TenantModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TenantModel?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    Task<TenantModel?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<TenantModel> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);

    Task<TenantModel?> UpdateAsync(string tenantCode, UpdateTenantRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string tenantCode, CancellationToken cancellationToken = default);
}
