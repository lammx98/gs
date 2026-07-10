namespace GS.Core.Auth;

public sealed record AuthTokensResult(
    long UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
