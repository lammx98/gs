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
                SnowflakeIdOpenApiRewriter.ApplyStringSchema(schema);
            }
            else
            {
                SnowflakeIdOpenApiRewriter.ApplyStringItemsSchema(schema);
            }

            return Task.CompletedTask;
        }

        var parameter = context.ParameterDescription;
        if (parameter is not null && SnowflakeIdNaming.IsSnowflakeIdName(parameter.Name))
        {
            var type = parameter.Type ?? parameter.ModelMetadata?.ModelType;
            if ((type is not null && SnowflakeIdNaming.IsLongScalarType(type))
                || SnowflakeIdOpenApiRewriter.IsInt64Schema(schema))
            {
                SnowflakeIdOpenApiRewriter.ApplyStringSchema(schema);
            }
            else if (type is not null
                     && SnowflakeIdNaming.TryGetLongElementType(type, out _))
            {
                SnowflakeIdOpenApiRewriter.ApplyStringItemsSchema(schema);
            }

            return Task.CompletedTask;
        }

        // Named schemas such as ListOfInt64 are generated from List<long> without a
        // property name, so the *Ids convention above never runs. ID collection
        // properties $ref that shared schema, which is why they stayed integer[].
        if (context.JsonPropertyInfo is null
            && context.JsonTypeInfo is { Type: var clrType }
            && SnowflakeIdNaming.TryGetLongElementType(clrType, out _))
        {
            SnowflakeIdOpenApiRewriter.ApplyStringItemsSchema(schema);
        }

        return Task.CompletedTask;
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
            if (parameter is not OpenApiParameter { Schema: { } schema }
                || !SnowflakeIdNaming.IsSnowflakeIdName(parameter.Name))
            {
                continue;
            }

            SnowflakeIdOpenApiRewriter.RewriteIdSchema(schema);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Last pass after schemas have been collected into <c>components.schemas</c>.
/// Property-level transformers cannot rewrite a <c>$ref</c> to <c>ListOfInt64</c>;
/// this mutates that shared array schema (and any leftover inline *Ids arrays).
/// </summary>
internal sealed class SnowflakeIdOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public static SnowflakeIdOpenApiDocumentTransformer Instance { get; } = new();

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        SnowflakeIdOpenApiRewriter.RewriteDocument(document);
        return Task.CompletedTask;
    }
}

internal static class SnowflakeIdOpenApiRewriter
{
    public static void RewriteDocument(OpenApiDocument document)
    {
        if (document.Components?.Schemas is not { } schemas)
        {
            return;
        }

        foreach (var schema in schemas.Values)
        {
            // Shared List<long> schemas (ListOfInt64, IReadOnlyListOfInt64, ...)
            if (schema is OpenApiSchema concrete && IsInt64ArraySchema(concrete))
            {
                ApplyStringItemsSchema(concrete);
            }

            RewriteNestedIdProperties(schema);
        }
    }

    public static void RewriteIdSchema(IOpenApiSchema schema)
    {
        switch (schema)
        {
            case OpenApiSchema concrete when IsInt64Schema(concrete):
                ApplyStringSchema(concrete);
                break;
            case OpenApiSchema concrete when IsInt64ArraySchema(concrete):
                ApplyStringItemsSchema(concrete);
                break;
            case OpenApiSchemaReference { Target: OpenApiSchema target }:
                RewriteIdSchema(target);
                break;
        }
    }

    public static void ApplyStringSchema(OpenApiSchema schema)
    {
        var type = JsonSchemaType.String;
        if (HasNull(schema.Type))
        {
            type |= JsonSchemaType.Null;
        }

        schema.Type = type;
        schema.Format = null;
    }

    public static void ApplyStringItemsSchema(OpenApiSchema schema)
    {
        var type = JsonSchemaType.Array;
        if (HasNull(schema.Type))
        {
            type |= JsonSchemaType.Null;
        }

        schema.Type = type;
        schema.Format = null;

        if (schema.Items is OpenApiSchema items)
        {
            ApplyStringSchema(items);
            return;
        }

        schema.Items = new OpenApiSchema { Type = JsonSchemaType.String };
    }

    public static bool IsInt64Schema(IOpenApiSchema schema)
    {
        schema = Resolve(schema);
        return string.Equals(schema.Format, "int64", StringComparison.OrdinalIgnoreCase)
            || schema.Type is JsonSchemaType.Integer
                or (JsonSchemaType.Integer | JsonSchemaType.Null);
    }

    public static bool IsInt64ArraySchema(IOpenApiSchema schema)
    {
        schema = Resolve(schema);
        if (!IsArraySchema(schema) || schema.Items is null)
        {
            return false;
        }

        return IsInt64Schema(schema.Items);
    }

    private static void RewriteNestedIdProperties(IOpenApiSchema? schema)
    {
        if (schema is not OpenApiSchema concrete)
        {
            return;
        }

        if (concrete.Properties is { } properties)
        {
            foreach (var (name, property) in properties)
            {
                if (SnowflakeIdNaming.IsSnowflakeIdName(name))
                {
                    RewriteIdSchema(property);
                }

                RewriteNestedIdProperties(property);
            }
        }

        RewriteNestedIdProperties(concrete.Items);
        RewriteNestedIdProperties(concrete.Not);
        RewriteNestedIdProperties(concrete.AdditionalProperties);
        RewriteNestedIdProperties(concrete.AllOf);
        RewriteNestedIdProperties(concrete.OneOf);
        RewriteNestedIdProperties(concrete.AnyOf);
    }

    private static void RewriteNestedIdProperties(IList<IOpenApiSchema>? schemas)
    {
        if (schemas is null)
        {
            return;
        }

        foreach (var schema in schemas)
        {
            RewriteNestedIdProperties(schema);
        }
    }

    private static bool IsArraySchema(IOpenApiSchema schema)
    {
        schema = Resolve(schema);
        return schema.Type is JsonSchemaType.Array
            or (JsonSchemaType.Array | JsonSchemaType.Null)
            || schema.Items is not null;
    }

    private static IOpenApiSchema Resolve(IOpenApiSchema schema) =>
        schema is OpenApiSchemaReference { Target: { } target } ? target : schema;

    private static bool HasNull(JsonSchemaType? type) =>
        type is { } value && value.HasFlag(JsonSchemaType.Null);
}
