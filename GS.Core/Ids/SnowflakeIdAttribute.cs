namespace GS.Core.Ids;

/// <summary>
/// Marks a <see cref="long"/> / <see cref="Nullable{T}"/> property (or a collection of them)
/// as a snowflake ID for JSON string wire format.
/// Optional when the member is already named <c>Id</c> / <c>*Id</c> / <c>*Ids</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SnowflakeIdAttribute : Attribute;
