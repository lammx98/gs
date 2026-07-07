using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GS.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GS.Core.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AuthTokenResult CreateToken(JwtTokenRequest request)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiresMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new(_options.TenantClaimType, request.TenantId)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthTokenResult(
            request.UserId,
            request.Email,
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
