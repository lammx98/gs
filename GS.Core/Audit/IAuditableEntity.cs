namespace GS.Core.Audit;

/// <summary>Timestamp audit fields applied automatically on <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChanges"/>.</summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }

    DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Actor audit fields applied automatically when a <see cref="Auth.ICurrentUserAccessor"/> is available.</summary>
public interface IAuditableEntityWithUser : IAuditableEntity
{
    string? CreatedBy { get; set; }

    string? UpdatedBy { get; set; }
}
