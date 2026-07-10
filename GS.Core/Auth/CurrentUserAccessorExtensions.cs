using GS.Core.Results;

namespace GS.Core.Auth;

public static class CurrentUserAccessorExtensions
{
    /// <summary>Returns an unauthorized error when the request has no valid authenticated user.</summary>
    public static Error? GetAuthenticationError(this ICurrentUserAccessor accessor)
    {
        if (!accessor.IsAuthenticated || string.IsNullOrWhiteSpace(accessor.UserId))
        {
            return Error.Unauthorized("Authentication required.");
        }

        if (accessor.UserIdLong is null)
        {
            return Error.Unauthorized("Invalid user id in token.");
        }

        return null;
    }

    /// <summary>Maps JWT claims to <see cref="CurrentUserInfo"/>.</summary>
    public static Result<CurrentUserInfo> ToCurrentUserInfo(this ICurrentUserAccessor accessor)
    {
        var authError = accessor.GetAuthenticationError();
        if (authError is not null)
        {
            return Result<CurrentUserInfo>.Fail(authError);
        }

        if (string.IsNullOrWhiteSpace(accessor.Email))
        {
            return Result<CurrentUserInfo>.Fail(Error.Unauthorized("Email claim is missing from token."));
        }

        if (string.IsNullOrWhiteSpace(accessor.TenantId))
        {
            return Result<CurrentUserInfo>.Fail(Error.Unauthorized("Tenant claim is missing from token."));
        }

        return Result<CurrentUserInfo>.Success(new CurrentUserInfo
        {
            UserId = accessor.UserIdLong!.Value,
            Email = accessor.Email,
            TenantId = accessor.TenantId,
            DisplayName = null
        });
    }
}
