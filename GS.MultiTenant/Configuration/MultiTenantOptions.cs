namespace GS.MultiTenant.Configuration;

public sealed class MultiTenantOptions
{
    public const string SectionName = "MultiTenant";

    public string TenantHeaderName { get; set; } = "X-Tenant-Id";

    public string JwtTenantClaimType { get; set; } = "tenant_id";

    public string HostTemplate { get; set; } = "__tenant__.*";

    public string TenantServiceGrpcAddress { get; set; } = string.Empty;

    public bool RequireTenant { get; set; } = true;

    public bool UseRedisCache { get; set; }

    public string? RedisConnectionString { get; set; }

    public TimeSpan CacheAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan CacheStaleThreshold { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// PostgreSQL connection string for shared-database tenants (Basic, Standard).
    /// </summary>
    public string SharedDatabaseConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Short name of the current service, used in database naming (e.g. <c>clinical</c>).
    /// </summary>
    public string ServiceDatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Template for dedicated tenant databases. Tokens: <c>{tenantCode}</c>, <c>{tenantId}</c>, <c>{serviceName}</c>.
    /// </summary>
    public string DatabaseNamingTemplate { get; set; } = "{tenantCode}_{serviceName}";

    /// <summary>
    /// Configuration section containing database credentials keyed by <see cref="DefaultCredentialsRef"/> or tenant <c>CredentialsRef</c>.
    /// </summary>
    public string DatabaseCredentialsSection { get; set; } = "DatabaseCredentials";

    public string DefaultCredentialsRef { get; set; } = "default";

    public int DefaultDatabasePort { get; set; } = 5432;
}
