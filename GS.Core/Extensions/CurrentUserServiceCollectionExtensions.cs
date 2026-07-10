using GS.Core.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GS.Core.Extensions;

public static class CurrentUserServiceCollectionExtensions
{
    /// <summary>Registers <see cref="ICurrentUserAccessor"/> for reading the authenticated user from HTTP context.</summary>
    public static IServiceCollection AddCurrentUserAccessor(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        return services;
    }
}
