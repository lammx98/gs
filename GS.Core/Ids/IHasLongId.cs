namespace GS.Core.Ids;

/// <summary>Marker for entities whose <see cref="Id"/> is assigned by <see cref="ISnowflakeIdGenerator"/> on insert.</summary>
public interface IHasLongId
{
    long Id { get; set; }
}
