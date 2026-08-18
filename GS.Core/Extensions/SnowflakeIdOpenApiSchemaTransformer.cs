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

/// <summary>
/// Rewrites snowflake ID operation parameters after they have been materialized.
/// FastEndpoints route parameters do not always expose a ParameterDescription while
/// their schema is being generated, so the schema transformer alone cannot identify them.
/// </summary>
internal sealed class SnowflakeIdOpenApiOperationTransformer : IOpenApiOperationTransformer
{
    public static SnowflakeIdOpenApiOperationTransformer Instance { get; } = new();

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is OpenApiParameter { Schema: OpenApiSchema schema }
                && SnowflakeIdNaming.IsSnowflakeIdName(parameter.Name)
                && IsInt64Schema(schema))
            {
                schema.Type = JsonSchemaType.String;
                schema.Format = null;
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsInt64Schema(OpenApiSchema schema) =>
        string.Equals(schema.Format, "int64", StringComparison.OrdinalIgnoreCase)
        || schema.Type is JsonSchemaType.Integer
            or (JsonSchemaType.Integer | JsonSchemaType.Null);
}
