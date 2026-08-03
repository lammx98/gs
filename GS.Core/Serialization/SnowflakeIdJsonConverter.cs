using System.Buffers.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GS.Core.Serialization;

/// <summary>
/// Writes snowflake IDs as JSON strings (JS-safe) and reads either string or number tokens.
/// </summary>
public sealed class SnowflakeIdJsonConverter : JsonConverter<long>
{
    public static SnowflakeIdJsonConverter Instance { get; } = new();

    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ParseString(ref reader),
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.Null => throw new JsonException("Cannot convert null to a non-nullable snowflake id."),
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing a snowflake id.")
        };
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));

    internal static long ParseString(ref Utf8JsonReader reader)
    {
        if (!reader.HasValueSequence)
        {
            var span = reader.ValueSpan;
            if (Utf8Parser.TryParse(span, out long value, out var bytesConsumed)
                && bytesConsumed == span.Length)
            {
                return value;
            }
        }

        var text = reader.GetString();
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new JsonException($"Unable to parse '{text}' as a snowflake id.");
    }
}
