using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Configuration;
using GS.MultiTenant.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GS.MultiTenant.Services;

public sealed class PostgreSqlConnectionStringResolver : IConnectionStringResolver
{
    private readonly MultiTenantOptions _options;
    private readonly IConfiguration _configuration;

    public PostgreSqlConnectionStringResolver(
        IOptions<MultiTenantOptions> options,
        IConfiguration configuration)
    {
        _options = options.Value;
        _configuration = configuration;
    }

    public bool UsesDedicatedDatabase(TenantModel? tenant) =>
        tenant is not null && tenant.UsesDedicatedDatabase;

    public string ResolveShared()
    {
        if (string.IsNullOrWhiteSpace(_options.SharedDatabaseConnectionString))
        {
            throw new InvalidOperationException(
                "MultiTenant:SharedDatabaseConnectionString is not configured.");
        }

        return _options.SharedDatabaseConnectionString;
    }

    public string ResolveDedicated(TenantModel tenant)
    {
        if (!tenant.UsesDedicatedDatabase)
        {
            throw new InvalidOperationException(
                $"Tenant '{tenant.TenantCode}' does not use a dedicated database.");
        }

        if (string.IsNullOrWhiteSpace(tenant.DatabaseHost))
        {
            throw new InvalidOperationException(
                $"Tenant '{tenant.TenantCode}' is Premium but DatabaseHost is not configured.");
        }

        var credentials = ResolveCredentials(tenant.CredentialsRef);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = tenant.DatabaseHost.Trim(),
            Port = tenant.DatabasePort ?? _options.DefaultDatabasePort,
            Database = BuildDatabaseName(tenant),
            Username = credentials.Username,
            Password = credentials.Password
        };

        return builder.ConnectionString;
    }

    private string BuildDatabaseName(TenantModel tenant)
    {
        if (string.IsNullOrWhiteSpace(_options.ServiceDatabaseName))
        {
            throw new InvalidOperationException(
                "MultiTenant:ServiceDatabaseName must be configured for dedicated database tenants.");
        }

        return _options.DatabaseNamingTemplate
            .Replace("{tenantCode}", tenant.TenantCode, StringComparison.Ordinal)
            .Replace("{tenantId}", tenant.TenantId, StringComparison.Ordinal)
            .Replace("{serviceName}", _options.ServiceDatabaseName, StringComparison.Ordinal);
    }

    private DatabaseCredentialSettings ResolveCredentials(string? credentialsRef)
    {
        var refKey = string.IsNullOrWhiteSpace(credentialsRef)
            ? _options.DefaultCredentialsRef
            : credentialsRef.Trim();

        var credentials = _configuration
            .GetSection($"{_options.DatabaseCredentialsSection}:{refKey}")
            .Get<DatabaseCredentialSettings>();

        if (credentials is null || string.IsNullOrWhiteSpace(credentials.Username))
        {
            throw new InvalidOperationException(
                $"Database credentials '{refKey}' were not found under section '{_options.DatabaseCredentialsSection}'.");
        }

        return credentials;
    }
}
