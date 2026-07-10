namespace GS.Core.Audit;

/// <summary>Entity with automatic timestamp audit fields.</summary>
public abstract class AuditableEntity : IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Entity with timestamp and actor audit fields.</summary>
public abstract class AuditableEntityWithUser : AuditableEntity, IAuditableEntityWithUser
{
    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
}
