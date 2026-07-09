using GS.Core.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace GS.Core.Extensions;

public static class GrpcWebApplicationBuilderExtensions
{
    /// <summary>
    /// Listens on the default HTTP port for REST and a dedicated HTTP/2 port for gRPC.
    /// </summary>
    public static WebApplicationBuilder ConfigureGsKestrelForGrpc(
        this WebApplicationBuilder builder,
        int? httpPort = null)
    {
        var grpcOptions = builder.Configuration.GetSection(GrpcOptions.SectionName).Get<GrpcOptions>()
            ?? new GrpcOptions();

        builder.WebHost.ConfigureKestrel(options =>
        {
            if (httpPort.HasValue)
            {
                options.ListenAnyIP(httpPort.Value);
            }

            options.ListenAnyIP(grpcOptions.ServerPort, listen => listen.Protocols = HttpProtocols.Http2);
        });

        return builder;
    }
}
