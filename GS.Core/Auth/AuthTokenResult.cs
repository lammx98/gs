namespace GS.Core.Auth;

public sealed record AuthTokenResult(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAt);
