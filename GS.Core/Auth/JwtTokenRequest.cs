namespace GS.Core.Auth;

public sealed record JwtTokenRequest(
    Guid UserId,
    string Email,
    string TenantId);
