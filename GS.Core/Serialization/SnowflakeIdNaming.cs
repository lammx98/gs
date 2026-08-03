using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using GS.Core.Ids;

namespace GS.Core.Serialization;

/// <summary>
/// Convention helpers for identifying snowflake ID members without converting every <see cref="long"/>.
/// Matches <c>Id</c> and PascalCase/camelCase names ending in <c>Id</c> (e.g. <c>PatientId</c>),
/// plus members annotated with <see cref="SnowflakeIdAttribute"/>.
/// </summary>
public static class SnowflakeIdNaming
{
    public static bool IsSnowflakeIdJsonProperty(JsonPropertyInfo property)
    {
        if (property.PropertyType != typeof(long) && property.PropertyType != typeof(long?))
        {
            return false;
        }

        if (HasAttribute<NotSnowflakeIdAttribute>(property.AttributeProvider))
        {
            return false;
        }

        if (HasAttribute<SnowflakeIdAttribute>(property.AttributeProvider))
        {
            return true;
        }

        return IsSnowflakeIdName(GetClrMemberName(property));
    }

    public static bool IsSnowflakeIdName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (name.Equals("Id", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Use ordinal suffix checks so names like "Paid" are not treated as IDs.
        return name.EndsWith("Id", StringComparison.Ordinal)
            || name.EndsWith("ID", StringComparison.Ordinal);
    }

    private static string GetClrMemberName(JsonPropertyInfo property) =>
        property.AttributeProvider switch
        {
            PropertyInfo pi => pi.Name,
            FieldInfo fi => fi.Name,
            ParameterInfo pai => pai.Name ?? property.Name,
            _ => property.Name
        };

    private static bool HasAttribute<TAttribute>(ICustomAttributeProvider? provider)
        where TAttribute : Attribute =>
        provider?.IsDefined(typeof(TAttribute), inherit: true) == true;
}
