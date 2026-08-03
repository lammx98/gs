using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GS.Core.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace GS.Core.Extensions;

public static class SnowflakeIdJsonSerializationExtensions
{
    /// <summary>
    /// Registers snowflake ID string serialization for ASP.NET Core HTTP JSON and MVC JSON options.
    /// FastEndpoints hosts should also call
    /// <c>config.Serializer.ConfigureSnowflakeIdsAsStrings()</c> and
    /// <c>options.MapSnowflakeIdsAsStrings()</c> via OpenAPI document configuration.
    /// </summary>
    public static IServiceCollection AddSnowflakeIdJsonSerialization(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(static options =>
            options.SerializerOptions.ConfigureSnowflakeIdsAsStrings());

        services.Configure<MvcJsonOptions>(static options =>
            options.JsonSerializerOptions.ConfigureSnowflakeIdsAsStrings());

        return services;
    }

    /// <summary>
    /// Applies snowflake ID string converters to the given <see cref="JsonSerializerOptions"/>.
    /// Only members named <c>Id</c> / <c>*Id</c> (or annotated with
    /// <see cref="Ids.SnowflakeIdAttribute"/>) are written/read as strings — other
    /// <see cref="long"/> values remain JSON numbers.
    /// </summary>
    public static JsonSerializerOptions ConfigureSnowflakeIdsAsStrings(this JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(SnowflakeIdJsonTypeInfoModifiers.ApplySnowflakeIdConverters);

        return options;
    }

    /// <summary>
    /// Convenience for FastEndpoints: <c>config.Serializer.ConfigureSnowflakeIdsAsStrings()</c>.
    /// </summary>
    public static FastEndpoints.SerializerOptions ConfigureSnowflakeIdsAsStrings(
        this FastEndpoints.SerializerOptions serializerOptions)
    {
        ArgumentNullException.ThrowIfNull(serializerOptions);
        serializerOptions.Options.ConfigureSnowflakeIdsAsStrings();
        return serializerOptions;
    }

    /// <summary>
    /// Maps snowflake ID schemas to OpenAPI <c>string</c> so generated clients stay JS-safe.
    /// </summary>
    public static OpenApiOptions MapSnowflakeIdsAsStrings(this OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddSchemaTransformer(SnowflakeIdOpenApiSchemaTransformer.Instance);
        return options;
    }
}
