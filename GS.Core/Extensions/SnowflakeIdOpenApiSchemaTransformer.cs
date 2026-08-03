using GS.Core.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GS.Core.Extensions;

/// <summary>
/// Rewrites OpenAPI schemas for snowflake ID members from int64 to string.
/// </summary>
internal sealed class SnowflakeIdOpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    public static SnowflakeIdOpenApiSchemaTransformer Instance { get; } = new();

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonPropertyInfo is { } property
            && SnowflakeIdNaming.IsSnowflakeIdJsonProperty(property))
        {
            ApplyStringSchema(schema);
            return Task.CompletedTask;
        }

        var parameter = context.ParameterDescription;
        if (parameter is not null && SnowflakeIdNaming.IsSnowflakeIdName(parameter.Name))
        {
            var type = parameter.Type ?? parameter.ModelMetadata?.ModelType;
            if (IsLongType(type) || IsInt64Schema(schema))
            {
                ApplyStringSchema(schema);
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsLongType(Type? type) =>
        type == typeof(long) || type == typeof(long?);

    private static bool IsInt64Schema(OpenApiSchema schema) =>
        string.Equals(schema.Format, "int64", StringComparison.OrdinalIgnoreCase)
        || schema.Type is JsonSchemaType.Integer
            or (JsonSchemaType.Integer | JsonSchemaType.Null);

    private static void ApplyStringSchema(OpenApiSchema schema)
    {
        schema.Type = JsonSchemaType.String;
        schema.Format = null;
    }
}
