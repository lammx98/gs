using GS.Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GS.Core.Extensions;

public static class ResultMvcExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value!;
        }

        return ToProblemDetails(result.Errors);
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToProblemDetails(result.Errors);
    }

    private static ActionResult ToProblemDetails(IReadOnlyList<Error> errors)
    {
        var error = errors[0];
        var statusCode = error.StatusCode ?? StatusCodes.Status400BadRequest;

        return new ObjectResult(new
        {
            error = error.Code,
            message = error.Message,
            errors = errors.Select(static e => new { e.Code, e.Message }).ToArray()
        })
        {
            StatusCode = statusCode
        };
    }
}
