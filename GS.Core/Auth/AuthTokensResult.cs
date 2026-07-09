namespace GS.Core.Auth;

public sealed record AuthTokensResult(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
