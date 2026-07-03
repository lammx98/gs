namespace GS.Core.Results;

public static class ResultExtensions
{
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map) =>
        result.IsSuccess
            ? Result<TOut>.Success(map(result.Value!))
            : Result<TOut>.Fail(result.Errors.ToArray());

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind) =>
        result.IsSuccess
            ? bind(result.Value!)
            : Result<TOut>.Fail(result.Errors.ToArray());

    public static Result Bind<T>(this Result<T> result, Func<T, Result> bind) =>
        result.IsSuccess
            ? bind(result.Value!)
            : Result.Fail(result.Errors.ToArray());

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value!);
        }

        return result;
    }

    public static Result Tap(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error) =>
        result.IsSuccess && !predicate(result.Value!)
            ? Result<T>.Fail(error)
            : result;

    public static Result Ensure(this Result result, Func<bool> predicate, Error error) =>
        result.IsSuccess && !predicate()
            ? Result.Fail(error)
            : result;

    public static TResult Match<T, TResult>(
        this Result<T> result,
        Func<T, TResult> onSuccess,
        Func<IReadOnlyList<Error>, TResult> onFailure) =>
        result.IsSuccess
            ? onSuccess(result.Value!)
            : onFailure(result.Errors);

    public static TResult Match<TResult>(
        this Result result,
        Func<TResult> onSuccess,
        Func<IReadOnlyList<Error>, TResult> onFailure) =>
        result.IsSuccess
            ? onSuccess()
            : onFailure(result.Errors);

    public static async Task<Result<TOut>> Map<TIn, TOut>(
        this Task<Result<TIn>> task,
        Func<TIn, TOut> map) =>
        (await task.ConfigureAwait(false)).Map(map);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Task<Result<TIn>> task,
        Func<TIn, Result<TOut>> bind) =>
        (await task.ConfigureAwait(false)).Bind(bind);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Task<Result<TIn>> task,
        Func<TIn, Task<Result<TOut>>> bind)
    {
        var result = await task.ConfigureAwait(false);
        return result.IsSuccess
            ? await bind(result.Value!).ConfigureAwait(false)
            : Result<TOut>.Fail(result.Errors.ToArray());
    }

    public static async Task<Result> Bind<T>(
        this Task<Result<T>> task,
        Func<T, Task<Result>> bind)
    {
        var result = await task.ConfigureAwait(false);
        return result.IsSuccess
            ? await bind(result.Value!).ConfigureAwait(false)
            : Result.Fail(result.Errors.ToArray());
    }

    public static async Task<Result<T>> Ensure<T>(
        this Task<Result<T>> task,
        Func<T, bool> predicate,
        Error error)
    {
        var result = await task.ConfigureAwait(false);
        return result.Ensure(predicate, error);
    }
}
