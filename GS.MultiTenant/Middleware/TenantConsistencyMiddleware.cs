using GS.MultiTenant.Configuration;
using GS.MultiTenant.Models;
using GS.MultiTenant.Resolution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GS.MultiTenant.Middleware;

internal sealed class TenantConsistencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MultiTenantOptions _options;

    public TenantConsistencyMiddleware(RequestDelegate next, IOptions<MultiTenantOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, Finbuckle.MultiTenant.Abstractions.IMultiTenantContextAccessor<TenantModel> tenantAccessor)
    {
        var identifiers = new List<string>();

        var headerTenant = TenantIdentifierExtractor.FromHeader(context, _options);
        if (!string.IsNullOrWhiteSpace(headerTenant))
        {
            identifiers.Add(headerTenant);
        }

        var hostTenant = TenantIdentifierExtractor.FromHost(context, _options);
        if (!string.IsNullOrWhiteSpace(hostTenant))
        {
            identifiers.Add(hostTenant);
        }

        var jwtTenant = TenantIdentifierExtractor.FromJwt(context, _options);
        if (!string.IsNullOrWhiteSpace(jwtTenant))
        {
            identifiers.Add(jwtTenant);
        }

        TenantIdentifierExtractor.ValidateConsistency(identifiers);

        if (_options.RequireTenant && tenantAccessor.MultiTenantContext?.IsResolved != true
            && string.IsNullOrWhiteSpace(Messaging.TenantMessageContext.TenantId)
            && identifiers.Count == 0)
        {
            throw new Exceptions.TenantNotResolvedException();
        }

        await _next(context);
    }
}
