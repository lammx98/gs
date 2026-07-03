using GS.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GS.Core.Middleware;

public sealed class HttpStatusExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpStatusExceptionMiddleware> _logger;

    public HttpStatusExceptionMiddleware(RequestDelegate next, ILogger<HttpStatusExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpStatusException ex)
        {
            _logger.LogWarning(ex, "Request failed with status {StatusCode}", ex.StatusCode);
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = ex.GetType().Name,
                message = ex.Message
            });
        }
    }
}
