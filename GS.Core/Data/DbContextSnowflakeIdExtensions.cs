using GS.Core.Ids;
using Microsoft.EntityFrameworkCore;

namespace GS.Core.Data;

/// <summary>
/// Assigns Snowflake ids to tracked <see cref="IHasLongId"/> entities with <c>Id == 0</c> on insert.
/// </summary>
public static class DbContextSnowflakeIdExtensions
{
    public static void ApplySnowflakeIds(this DbContext context, ISnowflakeIdGenerator? idGenerator)
    {
        if (idGenerator is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<IHasLongId>())
        {
            if (entry.State is EntityState.Added && entry.Entity.Id == 0)
            {
                entry.Entity.Id = idGenerator.NextId();
            }
        }
    }
}
