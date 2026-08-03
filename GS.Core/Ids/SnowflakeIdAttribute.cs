namespace GS.Core.Ids;

/// <summary>
/// Marks a <see cref="long"/> / <see cref="Nullable{T}"/> property as a snowflake ID for JSON string wire format.
/// Optional when the member is already named <c>Id</c> or ends with <c>Id</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SnowflakeIdAttribute : Attribute;
