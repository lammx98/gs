namespace GS.Core.Ids;

/// <summary>
/// Excludes a <see cref="long"/> / <see cref="Nullable{T}"/> member (or a collection of them)
/// from snowflake JSON string serialization even when its name matches the
/// <c>Id</c> / <c>*Id</c> / <c>*Ids</c> convention.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotSnowflakeIdAttribute : Attribute;
