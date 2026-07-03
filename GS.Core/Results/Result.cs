using GS.Core.Exceptions;

namespace GS.Core.Results;

/// <summary>
/// Non-generic result for operations without a return value.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new ArgumentException("A successful result cannot contain errors.", nameof(errors));
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new ArgumentException("A failed result must contain at least one error.", nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    public Error FirstError => Errors[0];

    public static Result Ok() => new(true, []);

    public static Result Fail(Error error) => new(false, [error]);

    public static Result Fail(params Error[] errors) => new(false, errors);

    public static Result Fail(string code, string message, int? statusCode = null) =>
        Fail(Error.Create(code, message, statusCode));

    public static Result Combine(params Result[] results)
    {
        var errors = results.Where(result => result.IsFailure).SelectMany(result => result.Errors).ToArray();
        return errors.Length == 0 ? Ok() : Fail(errors);
    }

    public HttpStatusException ToHttpStatusException()
    {
        var error = FirstError;
        return new HttpStatusException(error.Message, error.StatusCode ?? 400);
    }
}

/// <summary>
/// Result carrying a value on success, or one or more errors on failure.
/// </summary>
public sealed class Result<T> : Result
{
    private Result(T value) : base(true, [])
    {
        Value = value;
    }

    private Result(IReadOnlyList<Error> errors) : base(false, errors)
    {
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Fail(Error error) => new([error]);

    public static new Result<T> Fail(params Error[] errors) => new(errors);

    public static new Result<T> Fail(string code, string message, int? statusCode = null) =>
        Fail(Error.Create(code, message, statusCode));

    public T ValueOrThrow()
    {
        if (IsFailure)
        {
            throw ToHttpStatusException();
        }

        return Value!;
    }
}
