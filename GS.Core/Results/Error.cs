namespace GS.Core.Results;

/// <summary>
/// Structured failure descriptor for <see cref="Result"/> / <see cref="Result{T}"/>.
/// </summary>
public sealed record Error
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    /// <summary>
    /// Optional HTTP status when mapped at the API boundary.
    /// </summary>
    public int? StatusCode { get; init; }

    public static Error Create(string code, string message, int? statusCode = null) =>
        new() { Code = code, Message = message, StatusCode = statusCode };

    public static Error Validation(string message) =>
        Create("Validation", message, 400);

    public static Error NotFound(string message) =>
        Create("NotFound", message, 404);

    public static Error Conflict(string message) =>
        Create("Conflict", message, 409);

    public static Error Forbidden(string message) =>
        Create("Forbidden", message, 403);

    public static Error Unauthorized(string message) =>
        Create("Unauthorized", message, 401);
}
