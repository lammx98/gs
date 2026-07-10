namespace GS.Core.Configuration;

public sealed class SnowflakeOptions
{
    public const string SectionName = "Snowflake";

    /// <summary>Worker id (0–31). Must be unique per process instance in the same datacenter.</summary>
    public int WorkerId { get; set; }

    /// <summary>Datacenter id (0–31). Must be unique per deployment site.</summary>
    public int DatacenterId { get; set; }
}
