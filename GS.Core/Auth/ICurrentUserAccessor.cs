namespace GS.Core.Auth;

/// <summary>Reads the authenticated user from the current HTTP request (JWT claims).</summary>
public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }

    /// <summary>User id from JWT <c>sub</c> / name identifier claim.</summary>
    string? UserId { get; }

    /// <summary>Parsed <see cref="UserId"/> when it is a valid long.</summary>
    long? UserIdLong { get; }

    /// <summary>Email from JWT <c>email</c> claim.</summary>
    string? Email { get; }

    /// <summary>Tenant id from JWT tenant claim (default <c>tenant_id</c>).</summary>
    string? TenantId { get; }
}
