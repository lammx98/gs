namespace GS.Core.Exceptions;

/// <summary>
/// Application exception mapped to an HTTP status code.
/// </summary>
public class HttpStatusException : Exception
{
    public int StatusCode { get; }

    public HttpStatusException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
