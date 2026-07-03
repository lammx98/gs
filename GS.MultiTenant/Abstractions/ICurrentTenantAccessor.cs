using GS.MultiTenant.Abstractions;
using GS.MultiTenant.Models;

namespace GS.MultiTenant.Abstractions;

public interface ICurrentTenantAccessor
{
    /// <summary>Internal tenant id (<see cref="TenantModel.Id"/>).</summary>
    string? TenantId { get; }

    /// <summary>External code from URL/header before or after store lookup (<see cref="TenantModel.Identifier"/>).</summary>
    string? TenantCode { get; }

    TenantModel? Current { get; }

    TenantTier? Tier { get; }

    bool IsResolved { get; }
}
