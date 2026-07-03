using GS.Core.Middleware;
using Microsoft.AspNetCore.Builder;

namespace GS.Core.Extensions;

public static class HttpStatusExceptionApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHttpStatusExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<HttpStatusExceptionMiddleware>();
}
