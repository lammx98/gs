namespace GS.Core.Configuration;

public sealed class GrpcOptions
{
    public const string SectionName = "Grpc";

    /// <summary>
    /// Dedicated HTTP/2 port for gRPC endpoints. REST stays on the default ASP.NET Core port.
    /// </summary>
    public int ServerPort { get; set; } = 5001;

    public bool EnableDetailedErrors { get; set; }
}
