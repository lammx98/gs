using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GS.Core.Serialization;

/// <summary>
/// Nullable counterpart of <see cref="SnowflakeIdJsonConverter"/>.
/// </summary>
public sealed class NullableSnowflakeIdJsonConverter : JsonConverter<long?>
{
    public static NullableSnowflakeIdJsonConverter Instance { get; } = new();

    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return null;
        }

        return reader.TokenType switch
        {
            JsonTokenType.String => SnowflakeIdJsonConverter.ParseString(ref reader),
            JsonTokenType.Number => reader.GetInt64(),
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing a snowflake id.")
        };
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
    }
}
