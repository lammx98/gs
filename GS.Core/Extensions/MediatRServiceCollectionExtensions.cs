using System.Reflection;
using FluentValidation;
using GS.Core.Mediation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GS.Core.Extensions;

public static class MediatRServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR, FluentValidation validators, and the Result-aware validation pipeline behavior.
    /// </summary>
    public static IServiceCollection AddMediatR(
        this IServiceCollection services,
        Assembly assembly)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
