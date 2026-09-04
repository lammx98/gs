using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using GS.Core.Ids;

namespace GS.Core.Serialization;

/// <summary>
/// Convention helpers for identifying snowflake ID members without converting every <see cref="long"/>.
/// Matches <c>Id</c> and PascalCase/camelCase names ending in <c>Id</c> / <c>Ids</c>
/// (e.g. <c>PatientId</c>, <c>DiagnosisIds</c>), plus members annotated with
/// <see cref="SnowflakeIdAttribute"/>.
/// </summary>
public static class SnowflakeIdNaming
{
    public static bool IsSnowflakeIdJsonProperty(JsonPropertyInfo property)
    {
        if (HasAttribute<NotSnowflakeIdAttribute>(property.AttributeProvider))
        {
            return false;
        }

        var isScalar = IsLongScalarType(property.PropertyType);
        var isCollection = !isScalar && TryGetLongElementType(property.PropertyType, out _);
        if (!isScalar && !isCollection)
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

        if (name.Equals("Id", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Ids", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Use ordinal suffix checks so names like "Paid" are not treated as IDs.
        // "*Ids" is covered by the "*Id" suffix (e.g. DiagnosisIds).
        return name.EndsWith("Id", StringComparison.Ordinal)
            || name.EndsWith("ID", StringComparison.Ordinal)
            || name.EndsWith("IDs", StringComparison.Ordinal);
    }

    public static bool IsLongScalarType(Type type) =>
        type == typeof(long) || type == typeof(long?);

    /// <summary>
    /// Returns true when <paramref name="type"/> is an array or common collection of
    /// <see cref="long"/> / <see cref="Nullable{T}"/>.
    /// </summary>
    public static bool TryGetLongElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            if (IsLongScalarType(elementType))
            {
                return true;
            }

            elementType = null!;
            return false;
        }

        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length == 1 && IsLongScalarType(genericArgs[0])
                && IsSupportedCollectionDefinition(type.GetGenericTypeDefinition()))
            {
                elementType = genericArgs[0];
                return true;
            }
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType
                || iface.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                continue;
            }

            var arg = iface.GetGenericArguments()[0];
            if (!IsLongScalarType(arg))
            {
                continue;
            }

            elementType = arg;
            return true;
        }

        elementType = null!;
        return false;
    }

    private static bool IsSupportedCollectionDefinition(Type genericTypeDefinition) =>
        genericTypeDefinition == typeof(List<>)
        || genericTypeDefinition == typeof(IList<>)
        || genericTypeDefinition == typeof(ICollection<>)
        || genericTypeDefinition == typeof(IEnumerable<>)
        || genericTypeDefinition == typeof(IReadOnlyList<>)
        || genericTypeDefinition == typeof(IReadOnlyCollection<>)
        || genericTypeDefinition == typeof(HashSet<>)
        || genericTypeDefinition == typeof(ISet<>);

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
