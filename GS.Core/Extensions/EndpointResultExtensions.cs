using FastEndpoints;
using GS.Core.Results;
using Microsoft.AspNetCore.Http;

namespace GS.Core.Extensions;

public static class EndpointResultExtensions
{
    public static async Task SendResultAsync<TRequest, TResponse>(
        this global::FastEndpoints.Endpoint<TRequest, TResponse> endpoint,
        Result<TResponse> result,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        if (result.IsSuccess)
        {
            await endpoint.HttpContext.Response.SendAsync(
                result.Value,
                cancellation: cancellationToken);
            return;
        }

        var error = result.FirstError;

        await endpoint.HttpContext.Response.SendAsync(
            new
            {
                error = error.Code,
                message = error.Message,
                errors = result.Errors.Select(static e => new { e.Code, e.Message }).ToArray()
            },
            statusCode: error.StatusCode ?? StatusCodes.Status400BadRequest,
            cancellation: cancellationToken);
    }
}
