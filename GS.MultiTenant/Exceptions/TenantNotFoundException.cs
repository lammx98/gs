using GS.Core.Exceptions;

namespace GS.MultiTenant.Exceptions;

public sealed class TenantNotFoundException : HttpStatusException
{
    public TenantNotFoundException(string tenantId)
        : base($"Tenant '{tenantId}' was not found.", 404)
    {
    }
}
