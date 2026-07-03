using GS.MultiTenant.Messaging.MassTransit;
using MassTransit;

namespace GS.MultiTenant.Extensions;

public static class MassTransitTenantExtensions
{
    /// <summary>
    /// Adds tenant consume filters to all registered endpoints.
    /// </summary>
    public static void UseTenantPropagation(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddConfigureEndpointsCallback((context, _, endpoint) =>
        {
            endpoint.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
        });
    }

    /// <summary>
    /// Adds tenant publish filter. Call inside transport configuration (UsingRabbitMq, UsingKafka, etc.).
    /// </summary>
    public static void UseTenantPublishPropagation(this IPublishPipelineConfigurator configurator, IRegistrationContext context)
    {
        configurator.UsePublishFilter(typeof(TenantPublishFilter<>), context);
    }
}
