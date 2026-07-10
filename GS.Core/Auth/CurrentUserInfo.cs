namespace GS.Core.Auth;

/// <summary>Authenticated account snapshot shared across microservices.</summary>
public sealed record CurrentUserInfo
{
    public required long UserId { get; init; }

    public required string Email { get; init; }

    public required string TenantId { get; init; }

    public string? DisplayName { get; init; }
}
