using GS.MultiTenant.Grpc.Tenant;
using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Configuration;
using GS.MultiTenant.Mapping;
using GS.MultiTenant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GS.MultiTenant.Stores;

public sealed class GrpcTenantConfigurationClient : ITenantConfigurationClient
{
    private readonly MultiTenantOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public GrpcTenantConfigurationClient(
        IOptions<MultiTenantOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
    }

    public async Task<TenantModel?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_options.TenantServiceGrpcAddress))
        {
            return CreateDevTenant(tenantCode);
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetService<TenantResolver.TenantResolverClient>();
        if (client is null)
        {
            return null;
        }

        var response = await client.GetByTenantCodeAsync(
            new GetByTenantCodeRequest { TenantCode = tenantCode },
            cancellationToken: cancellationToken).ResponseAsync;

        return response.Found ? TenantGrpcMapper.ToModel(response.Tenant) : null;
    }

    public async Task<TenantModel?> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_options.TenantServiceGrpcAddress))
        {
            return CreateDevTenant(tenantId);
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetService<TenantResolver.TenantResolverClient>();
        if (client is null)
        {
            return null;
        }

        var response = await client.GetByTenantIdAsync(
            new GetByTenantIdRequest { TenantId = tenantId },
            cancellationToken: cancellationToken).ResponseAsync;

        return response.Found ? TenantGrpcMapper.ToModel(response.Tenant) : null;
    }

    private static TenantModel CreateDevTenant(string identifier) => new()
    {
        Id = identifier,
        Identifier = identifier,
        TenantName = identifier,
        Tier = Models.TenantTier.Basic
    };
}
