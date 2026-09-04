using System.Text.Json.Serialization.Metadata;

namespace GS.Core.Serialization;

/// <summary>
/// Applies snowflake string converters to matching scalar and collection ID members.
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

            if (property.PropertyType == typeof(long))
            {
                property.CustomConverter = SnowflakeIdJsonConverter.Instance;
            }
            else if (property.PropertyType == typeof(long?))
            {
                property.CustomConverter = NullableSnowflakeIdJsonConverter.Instance;
            }
            else
            {
                property.CustomConverter = SnowflakeIdCollectionJsonConverterFactory.Instance
                    .CreateConverter(property.PropertyType, typeInfo.Options);
            }
        }
    }
}
