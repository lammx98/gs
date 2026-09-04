using System.Text.Json;
using System.Text.Json.Serialization;

namespace GS.Core.Serialization;

/// <summary>
/// Factory that creates converters for collections of snowflake IDs written as JSON string arrays.
/// </summary>
public sealed class SnowflakeIdCollectionJsonConverterFactory : JsonConverterFactory
{
    public static SnowflakeIdCollectionJsonConverterFactory Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) =>
        SnowflakeIdNaming.TryGetLongElementType(typeToConvert, out _);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (!SnowflakeIdNaming.TryGetLongElementType(typeToConvert, out var elementType))
        {
            throw new InvalidOperationException(
                $"Type '{typeToConvert}' is not a supported snowflake id collection.");
        }

        var converterType = elementType == typeof(long)
            ? typeof(SnowflakeIdCollectionJsonConverter<>).MakeGenericType(typeToConvert)
            : typeof(NullableSnowflakeIdCollectionJsonConverter<>).MakeGenericType(typeToConvert);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Serializes a collection of <see cref="long"/> snowflake IDs as a JSON array of strings.
/// </summary>
public sealed class SnowflakeIdCollectionJsonConverter<TCollection> : JsonConverter<TCollection>
{
    public override TCollection? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType is not JsonTokenType.StartArray)
        {
            throw new JsonException(
                $"Unexpected token {reader.TokenType} when parsing a snowflake id collection.");
        }

        var buffer = new List<long>();
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray)
            {
                break;
            }

            buffer.Add(SnowflakeIdJsonConverter.Instance.Read(ref reader, typeof(long), options));
        }

        return SnowflakeIdCollectionMaterializer.Materialize<TCollection, long>(buffer);
    }

    public override void Write(
        Utf8JsonWriter writer,
        TCollection value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var id in (IEnumerable<long>)(object)value)
        {
            SnowflakeIdJsonConverter.Instance.Write(writer, id, options);
        }

        writer.WriteEndArray();
    }
}

/// <summary>
/// Serializes a collection of nullable <see cref="long"/> snowflake IDs as a JSON array of strings/nulls.
/// </summary>
public sealed class NullableSnowflakeIdCollectionJsonConverter<TCollection> : JsonConverter<TCollection>
{
    public override TCollection? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType is not JsonTokenType.StartArray)
        {
            throw new JsonException(
                $"Unexpected token {reader.TokenType} when parsing a snowflake id collection.");
        }

        var buffer = new List<long?>();
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray)
            {
                break;
            }

            buffer.Add(
                NullableSnowflakeIdJsonConverter.Instance.Read(ref reader, typeof(long?), options));
        }

        return SnowflakeIdCollectionMaterializer.Materialize<TCollection, long?>(buffer);
    }

    public override void Write(
        Utf8JsonWriter writer,
        TCollection value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var id in (IEnumerable<long?>)(object)value)
        {
            NullableSnowflakeIdJsonConverter.Instance.Write(writer, id, options);
        }

        writer.WriteEndArray();
    }
}

internal static class SnowflakeIdCollectionMaterializer
{
    public static TCollection Materialize<TCollection, TElement>(List<TElement> items)
    {
        var target = typeof(TCollection);

        if (target.IsArray)
        {
            var array = Array.CreateInstance(typeof(TElement), items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                array.SetValue(items[i], i);
            }

            return (TCollection)(object)array;
        }

        if (target.IsAssignableFrom(typeof(List<TElement>)))
        {
            return (TCollection)(object)items;
        }

        if (target.IsAssignableFrom(typeof(HashSet<TElement>)))
        {
            return (TCollection)(object)new HashSet<TElement>(items);
        }

        if (target.GetConstructor(Type.EmptyTypes) is { } ctor
            && typeof(ICollection<TElement>).IsAssignableFrom(target))
        {
            var collection = (ICollection<TElement>)ctor.Invoke(null);
            foreach (var item in items)
            {
                collection.Add(item);
            }

            return (TCollection)collection;
        }

        throw new NotSupportedException(
            $"Cannot materialize snowflake id collection of type '{target}'.");
    }
}
