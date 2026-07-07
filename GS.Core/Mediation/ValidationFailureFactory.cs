using FluentValidation;
using FluentValidation.Results;
using GS.Core.Results;

namespace GS.Core.Mediation;

internal static class ValidationFailureFactory
{
    public static TResponse FromFailures<TResponse>(IEnumerable<ValidationFailure> failures)
    {
        var message = string.Join("; ", failures.Select(failure => failure.ErrorMessage));
        var error = Error.Validation(message);

        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Fail(error);
        }

        if (typeof(TResponse).IsGenericType
            && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Fail), [typeof(Error)])!;

            return (TResponse)failMethod.Invoke(null, [error])!;
        }

        throw new ValidationException(failures);
    }
}
