namespace GS.Core.Auth;

public sealed record AuthTokenResult(
    long UserId,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAt);
