using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GS.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GS.Core.Auth;

internal sealed class CurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtOptions> jwtOptions) : ICurrentUserAccessor
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public string? UserId =>
        FindClaim(ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub);

    public long? UserIdLong =>
        long.TryParse(UserId, out var id) ? id : null;

    public string? Email =>
        FindClaim(ClaimTypes.Email, JwtRegisteredClaimNames.Email);

    public string? TenantId =>
        FindClaim(jwtOptions.Value.TenantClaimType, GsJwtClaimTypes.TenantId);

    private string? FindClaim(params string[] claimTypes)
    {
        if (Principal is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var value = Principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
