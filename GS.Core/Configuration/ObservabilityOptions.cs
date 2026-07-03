namespace GS.Core.Configuration;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string? ServiceName { get; set; }

    public string? ServiceVersion { get; set; }

    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();
}

public sealed class OpenTelemetryOptions
{
    public bool Enabled { get; set; } = true;

    public string? OtlpEndpoint { get; set; }

    public bool ExportTraces { get; set; } = true;

    public bool ExportMetrics { get; set; } = true;

    /// <summary>
    /// Adds EF Core span instrumentation (requires OpenTelemetry.Instrumentation.EntityFrameworkCore).
    /// </summary>
    public bool InstrumentEntityFrameworkCore { get; set; }
}
