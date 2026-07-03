using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Messaging;
using MassTransit;

namespace GS.MultiTenant.Messaging.MassTransit;

public sealed class TenantPublishFilter<T> : IFilter<PublishContext<T>>
    where T : class
{
    private readonly ICurrentTenantAccessor _tenantAccessor;

    public TenantPublishFilter(ICurrentTenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("tenantPublish");

    public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var tenantId = _tenantAccessor.TenantId;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            context.Headers.Set(TenantMessageHeaders.TenantId, tenantId);
        }

        await next.Send(context);
    }
}

public sealed class TenantConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    public void Probe(ProbeContext context) => context.CreateFilterScope("tenantConsume");

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        string? tenantId = null;
        if (context.Headers.TryGetHeader(TenantMessageHeaders.TenantId, out var headerValue))
        {
            tenantId = headerValue?.ToString();
        }

        if (string.IsNullOrWhiteSpace(tenantId) && context.Headers.TryGetHeader("tenant_id", out var legacy))
        {
            tenantId = legacy?.ToString();
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            await next.Send(context);
            return;
        }

        using var scope = TenantMessageContext.SetTenant(tenantId);
        await next.Send(context);
    }
}
