namespace GS.MultiTenant.Configuration;

public sealed class MultiTenantOptions
{
    public const string SectionName = "MultiTenant";

    public string TenantHeaderName { get; set; } = "X-Tenant-Id";

    public string JwtTenantClaimType { get; set; } = "tenant_id";

    public string HostTemplate { get; set; } = "__tenant__.*";

    public string TenantServiceBaseUrl { get; set; } = string.Empty;

    public string TenantServiceEndpointTemplate { get; set; } = "/api/tenants/{tenantCode}";

    public bool RequireTenant { get; set; } = true;

    public bool UseRedisCache { get; set; }

    public string? RedisConnectionString { get; set; }

    public TimeSpan CacheAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan CacheStaleThreshold { get; set; } = TimeSpan.FromMinutes(5);

    public string SharedDatabaseConnectionString { get; set; } = string.Empty;
}
