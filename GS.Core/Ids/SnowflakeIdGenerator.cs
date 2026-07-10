using GS.Core.Configuration;

namespace GS.Core.Ids;

/// <summary>
/// Twitter-style Snowflake id generator. Thread-safe via per-instance lock; safe for concurrent calls within one process.
/// Layout: 1 sign bit | 41 timestamp ms | 5 datacenter | 5 worker | 12 sequence.
/// </summary>
public sealed class SnowflakeIdGenerator : ISnowflakeIdGenerator
{
    private const long EpochMilliseconds = 1_609_459_200_000L; // 2021-01-01 UTC

    private const int WorkerIdBits = 5;
    private const int DatacenterIdBits = 5;
    private const int SequenceBits = 12;

    private const long MaxWorkerId = (1L << WorkerIdBits) - 1;
    private const long MaxDatacenterId = (1L << DatacenterIdBits) - 1;
    private const long SequenceMask = (1L << SequenceBits) - 1;

    private const int WorkerIdShift = SequenceBits;
    private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
    private const int TimestampShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

    private readonly long _workerId;
    private readonly long _datacenterId;
    private readonly object _sync = new();

    private long _lastTimestamp = -1L;
    private long _sequence;

    public SnowflakeIdGenerator(SnowflakeOptions options)
        : this(options.WorkerId, options.DatacenterId)
    {
    }

    public SnowflakeIdGenerator(int workerId, int datacenterId)
    {
        if (workerId < 0 || workerId > MaxWorkerId)
        {
            throw new ArgumentOutOfRangeException(nameof(workerId), $"Worker id must be between 0 and {MaxWorkerId}.");
        }

        if (datacenterId < 0 || datacenterId > MaxDatacenterId)
        {
            throw new ArgumentOutOfRangeException(nameof(datacenterId), $"Datacenter id must be between 0 and {MaxDatacenterId}.");
        }

        _workerId = workerId;
        _datacenterId = datacenterId;
    }

    public long NextId()
    {
        lock (_sync)
        {
            var timestamp = CurrentTimestamp();

            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException(
                    $"Clock moved backwards. Refusing to generate id for {_lastTimestamp - timestamp} ms.");
            }

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                {
                    timestamp = WaitNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - EpochMilliseconds) << TimestampShift)
                | (_datacenterId << DatacenterIdShift)
                | (_workerId << WorkerIdShift)
                | _sequence;
        }
    }

    private static long CurrentTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = CurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = CurrentTimestamp();
        }

        return timestamp;
    }
}
