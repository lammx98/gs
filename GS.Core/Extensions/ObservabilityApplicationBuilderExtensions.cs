using Microsoft.AspNetCore.Builder;
using Serilog;

namespace GS.Core.Extensions;

public static class ObservabilityApplicationBuilderExtensions
{
    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };
        });

        return app;
    }

    public static void RunWithObservability(this WebApplication app)
    {
        try
        {
            app.Run();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
