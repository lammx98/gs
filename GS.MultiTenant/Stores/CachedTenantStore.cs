using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Models;

namespace GS.MultiTenant.Stores;

public sealed class CachedTenantStore : Finbuckle.MultiTenant.Abstractions.IMultiTenantStore<TenantModel>
{
    private readonly ITenantResolutionService _tenantResolution;

    public CachedTenantStore(ITenantResolutionService tenantResolution)
    {
        _tenantResolution = tenantResolution;
    }

    public Task<bool> AddAsync(TenantModel tenantInfo) =>
        UpdateAsync(tenantInfo);

    public async Task<bool> UpdateAsync(TenantModel tenantInfo)
    {
        if (string.IsNullOrWhiteSpace(tenantInfo.Id))
        {
            return false;
        }

        await _tenantResolution.SetAsync(tenantInfo);
        return true;
    }

    public async Task<bool> RemoveAsync(string identifier)
    {
        await _tenantResolution.ClearAsync(identifier);
        return true;
    }

    public Task<TenantModel?> GetByIdentifierAsync(string identifier) =>
        GetAsync(identifier);

    public Task<TenantModel?> GetAsync(string identifier) =>
        Guid.TryParse(identifier, out _)
            ? _tenantResolution.GetByTenantIdAsync(identifier)
            : _tenantResolution.GetByTenantCodeAsync(identifier);

    public Task<IEnumerable<TenantModel>> GetAllAsync() =>
        Task.FromResult(Enumerable.Empty<TenantModel>());

    public Task<IEnumerable<TenantModel>> GetAllAsync(int take, int skip) =>
        Task.FromResult(Enumerable.Empty<TenantModel>());
}
