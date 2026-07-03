using Finbuckle.MultiTenant.AspNetCore.Extensions;
using GS.Core.Extensions;
using GS.MultiTenant.Middleware;
using Microsoft.AspNetCore.Builder;

namespace GS.MultiTenant.Extensions;

public static class MultiTenantApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        app.UseHttpStatusExceptionHandling();
        app.UseMultiTenant();
        app.UseMiddleware<TenantConsistencyMiddleware>();
        return app;
    }
}
