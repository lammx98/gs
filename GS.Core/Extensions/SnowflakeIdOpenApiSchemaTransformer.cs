using GS.Core.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GS.Core.Extensions;

/// <summary>
/// Rewrites OpenAPI schemas for snowflake ID members from int64 to string
/// (including arrays/lists of IDs).
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
            if (SnowflakeIdNaming.IsLongScalarType(property.PropertyType))
            {
                ApplyStringSchema(schema);
            }
            else
            {
                ApplyStringItemsSchema(schema);
            }

            return Task.CompletedTask;
        }

        var parameter = context.ParameterDescription;
        if (parameter is not null && SnowflakeIdNaming.IsSnowflakeIdName(parameter.Name))
        {
            var type = parameter.Type ?? parameter.ModelMetadata?.ModelType;
            if ((type is not null && SnowflakeIdNaming.IsLongScalarType(type))
                || IsInt64Schema(schema))
            {
                ApplyStringSchema(schema);
            }
            else if (type is not null
                     && SnowflakeIdNaming.TryGetLongElementType(type, out _))
            {
                ApplyStringItemsSchema(schema);
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsInt64Schema(OpenApiSchema schema) =>
        string.Equals(schema.Format, "int64", StringComparison.OrdinalIgnoreCase)
        || schema.Type is JsonSchemaType.Integer
            or (JsonSchemaType.Integer | JsonSchemaType.Null);

    private static void ApplyStringSchema(OpenApiSchema schema)
    {
        schema.Type = JsonSchemaType.String;
        schema.Format = null;
    }

    private static void ApplyStringItemsSchema(OpenApiSchema schema)
    {
        if (schema.Items is OpenApiSchema items)
        {
            ApplyStringSchema(items);
            return;
        }

        schema.Items = new OpenApiSchema { Type = JsonSchemaType.String };
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
            if (parameter is not OpenApiParameter { Schema: OpenApiSchema schema }
                || !SnowflakeIdNaming.IsSnowflakeIdName(parameter.Name))
            {
                continue;
            }

            if (IsInt64Schema(schema))
            {
                schema.Type = JsonSchemaType.String;
                schema.Format = null;
            }
            else if (IsArraySchema(schema))
            {
                if (schema.Items is OpenApiSchema items)
                {
                    items.Type = JsonSchemaType.String;
                    items.Format = null;
                }
                else
                {
                    schema.Items = new OpenApiSchema { Type = JsonSchemaType.String };
                }
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsInt64Schema(OpenApiSchema schema) =>
        string.Equals(schema.Format, "int64", StringComparison.OrdinalIgnoreCase)
        || schema.Type is JsonSchemaType.Integer
            or (JsonSchemaType.Integer | JsonSchemaType.Null);

    private static bool IsArraySchema(OpenApiSchema schema) =>
        schema.Type is JsonSchemaType.Array
            or (JsonSchemaType.Array | JsonSchemaType.Null)
        || schema.Items is not null;
}
