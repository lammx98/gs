using GS.Core.Audit;
using GS.Core.Auth;
using Microsoft.EntityFrameworkCore;

namespace GS.Core.Data;

/// <summary>
/// Automatic audit for <see cref="IAuditableEntity"/> / <see cref="IAuditableEntityWithUser"/>.
/// Call from <see cref="DbContext.SaveChanges()"/> / <see cref="DbContext.SaveChangesAsync(System.Threading.CancellationToken)"/>.
/// </summary>
public static class DbContextAuditExtensions
{
    /// <summary>
    /// Sets <c>CreatedAt</c>/<c>UpdatedAt</c> (and actor fields for <see cref="IAuditableEntityWithUser"/>) on tracked entities.
    /// </summary>
    public static void ApplyAutomaticAuditFields(this DbContext context, ICurrentUserAccessor? userAccessor)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var actor = userAccessor?.UserId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditableEntity audit)
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    audit.CreatedAt = utcNow;
                    audit.UpdatedAt = utcNow;
                    if (entry.Entity is IAuditableEntityWithUser auditWithUser)
                    {
                        auditWithUser.CreatedBy = actor;
                        auditWithUser.UpdatedBy = actor;
                    }

                    break;
                case EntityState.Modified:
                    audit.UpdatedAt = utcNow;
                    if (entry.Entity is IAuditableEntityWithUser auditWithUserModified)
                    {
                        auditWithUserModified.UpdatedBy = actor;
                    }

                    break;
            }
        }
    }
}
