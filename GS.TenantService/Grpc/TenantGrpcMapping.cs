using GS.MultiTenant.Grpc.Tenant;
using GS.MultiTenant.Models;
using ProtoTenantTier = GS.MultiTenant.Grpc.Tenant.TenantTier;

namespace GS.TenantService.Grpc;

internal static class TenantGrpcMapping
{
    public static TenantInfo ToMessage(TenantModel tenant)
    {
        var message = new TenantInfo
        {
            TenantId = tenant.Id,
            TenantCode = tenant.Identifier,
            TenantName = tenant.TenantName ?? string.Empty,
            Tier = ToTier(tenant.Tier),
            UsesDedicatedDatabase = tenant.UsesDedicatedDatabase
        };

        if (!string.IsNullOrWhiteSpace(tenant.DatabaseHost))
        {
            message.DatabaseHost = tenant.DatabaseHost;
        }

        if (tenant.DatabasePort.HasValue)
        {
            message.DatabasePort = tenant.DatabasePort.Value;
        }

        if (!string.IsNullOrWhiteSpace(tenant.CredentialsRef))
        {
            message.CredentialsRef = tenant.CredentialsRef;
        }

        return message;
    }

    private static ProtoTenantTier ToTier(GS.MultiTenant.Models.TenantTier tier) => tier switch
    {
        GS.MultiTenant.Models.TenantTier.Basic => ProtoTenantTier.Basic,
        GS.MultiTenant.Models.TenantTier.Standard => ProtoTenantTier.Standard,
        GS.MultiTenant.Models.TenantTier.Premium => ProtoTenantTier.Premium,
        _ => ProtoTenantTier.Basic
    };
}
