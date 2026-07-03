using GS.Core.Exceptions;

namespace GS.MultiTenant.Exceptions;

public sealed class TenantNotResolvedException : HttpStatusException
{
    public TenantNotResolvedException()
        : base("Tenant identifier is required but was not found in the request.", 400)
    {
    }

    public TenantNotResolvedException(string message)
        : base(message, 400)
    {
    }
}
