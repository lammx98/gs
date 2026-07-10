namespace GS.Core.Auth;

public sealed record JwtTokenRequest(
    long UserId,
    string Email,
    string TenantId);
