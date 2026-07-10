namespace GS.Core.Ids;

/// <summary>Generates cluster-unique, time-ordered 64-bit identifiers (Snowflake).</summary>
public interface ISnowflakeIdGenerator
{
    long NextId();
}
