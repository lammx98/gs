using GS.Core.Caching;
using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Models;

namespace GS.MultiTenant.Services;

public sealed class TenantResolutionService : ITenantResolutionService
{
    private readonly ILayeredCache _cache;
    private readonly ITenantConfigurationClient _client;

    public TenantResolutionService(
        ILayeredCache cache,
        ITenantConfigurationClient client)
    {
        _cache = cache;
        _client = client;
    }

    public async Task<TenantModel?> GetByTenantCodeAsync(
        string tenantCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return null;
        }

        var normalizedCode = tenantCode.Trim().ToLowerInvariant();
        var cached = await _cache.GetAsync<TenantModel>(
            BuildCodeKey(normalizedCode),
            CacheLookupStrategy.MemoryThenRedis,
            cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        var tenant = await _client.GetTenantAsync(normalizedCode, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        tenant.Identifier = string.IsNullOrWhiteSpace(tenant.Identifier) ? normalizedCode : tenant.Identifier;
        tenant.Id = string.IsNullOrWhiteSpace(tenant.Id) ? normalizedCode : tenant.Id;

        await CacheTenantAsync(tenant, cancellationToken);
        return tenant;
    }

    public Task<TenantModel?> GetByTenantIdAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Task.FromResult<TenantModel?>(null);
        }

        return _cache.GetAsync<TenantModel>(
            BuildIdKey(tenantId.Trim()),
            CacheLookupStrategy.MemoryThenRedis,
            cancellationToken: cancellationToken);
    }

    public Task SetAsync(TenantModel tenant, CancellationToken cancellationToken = default) =>
        CacheTenantAsync(tenant, cancellationToken);

    public async Task ClearAsync(
        string tenantCode,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(tenantCode))
        {
            await _cache.ClearAsync(
                BuildCodeKey(tenantCode.Trim().ToLowerInvariant()),
                cancellationToken: cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            await _cache.ClearAsync(BuildIdKey(tenantId.Trim()), cancellationToken: cancellationToken);
        }
    }

    private async Task CacheTenantAsync(TenantModel tenant, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tenant.TenantCode))
        {
            await _cache.SetAsync(
                BuildCodeKey(tenant.TenantCode),
                tenant,
                CacheStorageTarget.MemoryAndRedis,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(tenant.TenantId))
        {
            await _cache.SetAsync(
                BuildIdKey(tenant.TenantId),
                tenant,
                CacheStorageTarget.MemoryAndRedis,
                cancellationToken);
        }
    }

    private static string BuildCodeKey(string tenantCode) => $"tenant:code:{tenantCode}";

    private static string BuildIdKey(string tenantId) => $"tenant:id:{tenantId}";
}
