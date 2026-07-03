using Finbuckle.MultiTenant.Abstractions;
using System.Text.Json.Serialization;

namespace GS.MultiTenant.Models;

/// <summary>
/// Standard tenant model shared across all microservices.
/// </summary>
public class TenantModel : ITenantInfo
{
    /// <summary>
    /// Internal primary key (GUID). Used for DB isolation filter and internal references.
    /// </summary>
    [JsonPropertyName("tenantId")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// External-facing code from URL subdomain (e.g. <c>acme.domain.com</c> → <c>acme</c>).
    /// Finbuckle uses this as <see cref="Identifier"/> for tenant resolution.
    /// </summary>
    [JsonPropertyName("tenantCode")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("tenantName")]
    public string? TenantName { get; set; }

    public TenantTier Tier { get; set; } = TenantTier.Basic;

    public string? ConnectionString { get; set; }

    [JsonIgnore]
    public bool UsesDedicatedDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

    /// <summary>Alias of <see cref="Id"/> — matches documentation terminology.</summary>
    [JsonIgnore]
    public string TenantId
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>Alias of <see cref="Identifier"/> — matches documentation terminology.</summary>
    [JsonIgnore]
    public string TenantCode
    {
        get => Identifier;
        set => Identifier = value;
    }
}
