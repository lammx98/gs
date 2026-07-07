using GS.Core.Auth;

namespace GS.Core.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;

    /// <summary>
    /// JWT claim used for tenant resolution. Must match <c>MultiTenant:JwtTenantClaimType</c>.
    /// </summary>
    public string TenantClaimType { get; set; } = GsJwtClaimTypes.TenantId;
}
