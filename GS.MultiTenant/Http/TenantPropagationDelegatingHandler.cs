using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Messaging;

namespace GS.MultiTenant.Http;

public sealed class TenantPropagationDelegatingHandler : DelegatingHandler
{
    private readonly ICurrentTenantAccessor _tenantAccessor;

    public TenantPropagationDelegatingHandler(ICurrentTenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.TenantId;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            request.Headers.Remove(TenantMessageHeaders.TenantId);
            request.Headers.TryAddWithoutValidation(TenantMessageHeaders.TenantId, tenantId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
