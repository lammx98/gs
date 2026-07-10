using System.Reflection;
using GS.Core.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace GS.Core.Extensions;

public static class ObservabilityWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        string? serviceName = null,
        Action<ObservabilityOptions>? configure = null,
        Action<TracerProviderBuilder>? configureTracing = null)
    {
        builder.Services.Configure<ObservabilityOptions>(
            builder.Configuration.GetSection(ObservabilityOptions.SectionName));

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        var options = builder.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();
        configure?.Invoke(options);

        serviceName ??= options.ServiceName ?? builder.Environment.ApplicationName;
        var serviceVersion = options.ServiceVersion ?? GetEntryAssemblyVersion();

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", serviceName)
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services);

            if (!HasSerilogWriteTo(context.Configuration))
            {
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {Message:lj}{NewLine}{Exception}");
            }
        });

        if (options.OpenTelemetry.Enabled)
        {
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName, serviceVersion: serviceVersion)
                    .AddAttributes([
                        new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
                    ]))
                .WithTracing(tracing =>
                {
                    if (!options.OpenTelemetry.ExportTraces)
                    {
                        return;
                    }

                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();

                    if (options.OpenTelemetry.InstrumentEntityFrameworkCore)
                    {
                        tracing.AddEntityFrameworkCoreInstrumentation();
                    }

                    configureTracing?.Invoke(tracing);

                    if (!string.IsNullOrWhiteSpace(options.OpenTelemetry.OtlpEndpoint))
                    {
                        tracing.AddOtlpExporter(otlp =>
                            otlp.Endpoint = new Uri(options.OpenTelemetry.OtlpEndpoint));
                    }
                })
                .WithMetrics(metrics =>
                {
                    if (!options.OpenTelemetry.ExportMetrics)
                    {
                        return;
                    }

                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();

                    if (!string.IsNullOrWhiteSpace(options.OpenTelemetry.OtlpEndpoint))
                    {
                        metrics.AddOtlpExporter(otlp =>
                            otlp.Endpoint = new Uri(options.OpenTelemetry.OtlpEndpoint));
                    }
                });
        }

        return builder;
    }

    private static bool HasSerilogWriteTo(IConfiguration configuration) =>
        configuration.GetSection("Serilog:WriteTo").GetChildren().Any();

    private static string GetEntryAssemblyVersion() =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
}
