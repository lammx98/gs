namespace GS.MultiTenant.Abstractions;

/// <summary>
/// Marks an entity that can be either tenant-scoped or shared across all tenants.
/// A <see langword="null"/> <see cref="TenantId"/> denotes a shared record.
/// </summary>
public interface ITenantSharedEntity
{
    string? TenantId { get; set; }
}
