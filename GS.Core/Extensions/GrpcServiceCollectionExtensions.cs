using GS.Core.Configuration;
using Grpc.Net.Client;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GS.Core.Extensions;

public static class GrpcServiceCollectionExtensions
{
    /// <summary>
    /// Registers a typed gRPC client pointing at <paramref name="address"/>.
    /// </summary>
    public static IServiceCollection AddGrpcClient<TClient>(
        this IServiceCollection services,
        string address,
        Action<GrpcClientFactoryOptions>? configureClient = null,
        Action<IServiceProvider, GrpcChannelOptions>? configureChannel = null)
        where TClient : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var builder = services
            .AddGrpcClient<TClient>((_, options) =>
            {
                options.Address = new Uri(address);
                configureClient?.Invoke(options);
            })
            .ConfigureChannel((sp, options) =>
            {
                options.HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true
                };
                configureChannel?.Invoke(sp, options);
            });

        return services;
    }

    /// <summary>
    /// Registers gRPC server primitives and binds <see cref="GrpcOptions"/>.
    /// </summary>
    public static IServiceCollection AddGrpcServer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GrpcOptions>? configure = null)
    {
        services.Configure<GrpcOptions>(configuration.GetSection(GrpcOptions.SectionName));
        if (configure is not null)
        {
            services.Configure(configure);
        }

        var grpcOptions = configuration.GetSection(GrpcOptions.SectionName).Get<GrpcOptions>()
            ?? new GrpcOptions();
        configure?.Invoke(grpcOptions);

        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = grpcOptions.EnableDetailedErrors;
        });

        return services;
    }
}
