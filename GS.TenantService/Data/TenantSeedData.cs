using GS.MultiTenant.Models;

namespace GS.TenantService.Data;

internal static class TenantSeedData
{
    public static readonly TenantEntity[] Tenants =
    [
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            TenantCode = "acme",
            TenantName = "Acme Clinic",
            Tier = TenantTier.Basic,
            ConnectionString = null,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new()
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            TenantCode = "beta",
            TenantName = "Beta Health Center",
            Tier = TenantTier.Standard,
            ConnectionString = null,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            TenantCode = "vipcare",
            TenantName = "VIP Care Hospital",
            Tier = TenantTier.Vip,
            ConnectionString = "Server=vip-db.internal;Database=VipCare;Trusted_Connection=True;TrustServerCertificate=True",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        }
    ];
}
