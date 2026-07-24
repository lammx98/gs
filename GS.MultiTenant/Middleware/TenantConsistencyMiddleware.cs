using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Configuration;
using GS.MultiTenant.Models;
using GS.MultiTenant.Resolution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GS.MultiTenant.Middleware;

/// <summary>
/// Ensures all present tenant sources (header, host, JWT) refer to the same tenant.
/// Identifiers are resolved to <see cref="TenantModel.TenantId"/> before comparison —
/// so <c>acme</c> (code) and a JWT <c>tenant_id</c> GUID for the same tenant match.
/// </summary>
internal sealed class TenantConsistencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MultiTenantOptions _options;

    public TenantConsistencyMiddleware(RequestDelegate next, IOptions<MultiTenantOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        Finbuckle.MultiTenant.Abstractions.IMultiTenantContextAccessor<TenantModel> tenantAccessor,
        ITenantResolutionService tenantResolution)
    {
        var sources = new List<(string Source, string Value)>();

        var headerTenant = TenantIdentifierExtractor.FromHeader(context, _options);
        if (!string.IsNullOrWhiteSpace(headerTenant))
        {
            sources.Add(("header", headerTenant));
        }

        var hostTenant = TenantIdentifierExtractor.FromHost(context, _options);
        if (!string.IsNullOrWhiteSpace(hostTenant))
        {
            sources.Add(("host", hostTenant));
        }

        var jwtTenant = TenantIdentifierExtractor.FromJwt(context, _options);
        if (!string.IsNullOrWhiteSpace(jwtTenant))
        {
            sources.Add(("jwt", jwtTenant));
        }

        if (sources.Count > 0)
        {
            await ValidateResolvedConsistencyAsync(
                sources,
                tenantAccessor.MultiTenantContext?.TenantInfo,
                tenantResolution,
                context.RequestAborted);
        }

        if (_options.RequireTenant
            && tenantAccessor.MultiTenantContext?.IsResolved != true
            && string.IsNullOrWhiteSpace(Messaging.TenantMessageContext.TenantId)
            && sources.Count == 0)
        {
            throw new Exceptions.TenantNotResolvedException();
        }

        await _next(context);
    }

    private static async Task ValidateResolvedConsistencyAsync(
        IReadOnlyList<(string Source, string Value)> sources,
        TenantModel? alreadyResolved,
        ITenantResolutionService tenantResolution,
        CancellationToken cancellationToken)
    {
        var tenantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var labels = new List<string>();

        if (!string.IsNullOrWhiteSpace(alreadyResolved?.TenantId))
        {
            tenantIds.Add(alreadyResolved.TenantId);
            labels.Add($"resolved:{alreadyResolved.TenantCode}|{alreadyResolved.TenantId}");
        }

        foreach (var (source, value) in sources)
        {
            var tenant = await ResolveIdentifierAsync(tenantResolution, value, cancellationToken);
            if (tenant is null || string.IsNullOrWhiteSpace(tenant.TenantId))
            {
                throw new Exceptions.TenantMismatchException(
                    $"Unable to resolve tenant identifier '{value}' from {source}.");
            }

            tenantIds.Add(tenant.TenantId);
            labels.Add($"{source}:{value}→{tenant.TenantId}");
        }

        if (tenantIds.Count > 1)
        {
            throw new Exceptions.TenantMismatchException(
                $"Conflicting tenant identifiers detected: {string.Join(", ", labels)}.");
        }
    }

    private static Task<TenantModel?> ResolveIdentifierAsync(
        ITenantResolutionService tenantResolution,
        string identifier,
        CancellationToken cancellationToken) =>
        Guid.TryParse(identifier.Trim(), out _)
            ? tenantResolution.GetByTenantIdAsync(identifier.Trim(), cancellationToken)
            : tenantResolution.GetByTenantCodeAsync(identifier.Trim(), cancellationToken);
}
