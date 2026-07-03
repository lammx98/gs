using GS.MultiTenant.Configuration;
using GS.MultiTenant.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace GS.MultiTenant.Stores;

public sealed class HttpTenantConfigurationClient : Abstractions.ITenantConfigurationClient
{
    private readonly HttpClient _httpClient;
    private readonly MultiTenantOptions _options;

    public HttpTenantConfigurationClient(HttpClient httpClient, IOptions<MultiTenantOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<TenantModel?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // tenantId here is the resolved identifier (tenantCode from URL/header/JWT), not necessarily the internal PK yet.
        var tenantCode = tenantId;

        if (string.IsNullOrWhiteSpace(_options.TenantServiceBaseUrl))
        {
            return new TenantModel
            {
                Id = tenantCode,
                Identifier = tenantCode,
                TenantName = tenantCode,
                Tier = TenantTier.Basic
            };
        }

        var endpoint = _options.TenantServiceEndpointTemplate
            .Replace("{tenantCode}", tenantCode, StringComparison.Ordinal)
            .Replace("{tenantId}", tenantCode, StringComparison.Ordinal);
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var tenant = await response.Content.ReadFromJsonAsync<TenantModel>(cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        tenant.Id = string.IsNullOrWhiteSpace(tenant.Id) ? tenantCode : tenant.Id;
        tenant.Identifier = string.IsNullOrWhiteSpace(tenant.Identifier) ? tenantCode : tenant.Identifier;
        return tenant;
    }
}
