using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Configuration;
using GS.MultiTenant.Models;
using GS.Core.Caching;
using Microsoft.Extensions.Options;

namespace GS.MultiTenant.Stores;

public sealed class CachedTenantStore : Finbuckle.MultiTenant.Abstractions.IMultiTenantStore<TenantModel>
{
    private readonly ITenantConfigurationClient _client;
    private readonly StaleWhileRevalidateCache<TenantModel> _cache;
    private readonly MultiTenantOptions _options;

    public CachedTenantStore(
        ITenantConfigurationClient client,
        StaleWhileRevalidateCache<TenantModel> cache,
        IOptions<MultiTenantOptions> options)
    {
        _client = client;
        _cache = cache;
        _options = options.Value;
    }

    public Task<bool> AddAsync(TenantModel tenantInfo) =>
        UpdateAsync(tenantInfo);

    public async Task<bool> UpdateAsync(TenantModel tenantInfo)
    {
        if (string.IsNullOrWhiteSpace(tenantInfo.Id))
        {
            return false;
        }

        await _cache.SetAsync(GetCacheKey(tenantInfo.Id), tenantInfo);
        return true;
    }

    public Task<bool> RemoveAsync(string identifier)
    {
        _cache.Remove(GetCacheKey(identifier));
        return Task.FromResult(true);
    }

    public Task<TenantModel?> GetByIdentifierAsync(string identifier) =>
        GetAsync(identifier);

    public Task<TenantModel?> GetAsync(string identifier) =>
        _cache.GetOrCreateAsync(
            GetCacheKey(identifier),
            ct => _client.GetTenantAsync(identifier, ct));

    public Task<IEnumerable<TenantModel>> GetAllAsync() =>
        Task.FromResult(Enumerable.Empty<TenantModel>());

    public Task<IEnumerable<TenantModel>> GetAllAsync(int take, int skip) =>
        Task.FromResult(Enumerable.Empty<TenantModel>());

    private static string GetCacheKey(string identifier) => $"tenant:{identifier}";
}
