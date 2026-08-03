using System.Text.Json.Serialization.Metadata;

namespace GS.Core.Serialization;

/// <summary>
/// Applies snowflake string converters only to matching <see cref="long"/> / <see cref="Nullable{T}"/> members.
/// </summary>
public static class SnowflakeIdJsonTypeInfoModifiers
{
    public static void ApplySnowflakeIdConverters(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (!SnowflakeIdNaming.IsSnowflakeIdJsonProperty(property))
            {
                continue;
            }

            property.CustomConverter = property.PropertyType == typeof(long)
                ? SnowflakeIdJsonConverter.Instance
                : NullableSnowflakeIdJsonConverter.Instance;
        }
    }
}
