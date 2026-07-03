using GS.Core.Exceptions;

namespace GS.MultiTenant.Exceptions;

public sealed class TenantMismatchException : HttpStatusException
{
    public TenantMismatchException(string message)
        : base(message, 401)
    {
    }
}
