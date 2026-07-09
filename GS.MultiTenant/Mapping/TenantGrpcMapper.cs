using GS.MultiTenant.Grpc.Tenant;
using GS.MultiTenant.Models;
using ProtoTenantTier = GS.MultiTenant.Grpc.Tenant.TenantTier;

namespace GS.MultiTenant.Mapping;

internal static class TenantGrpcMapper
{
    public static TenantModel ToModel(TenantInfo message) => new()
    {
        Id = message.TenantId,
        Identifier = message.TenantCode,
        TenantName = string.IsNullOrWhiteSpace(message.TenantName) ? null : message.TenantName,
        Tier = ToTier(message.Tier),
        UsesDedicatedDatabase = message.UsesDedicatedDatabase,
        DatabaseHost = message.HasDatabaseHost ? message.DatabaseHost : null,
        DatabasePort = message.HasDatabasePort ? message.DatabasePort : null,
        CredentialsRef = message.HasCredentialsRef ? message.CredentialsRef : null
    };

    public static Models.TenantTier ToTier(ProtoTenantTier value) => value switch
    {
        ProtoTenantTier.Basic => Models.TenantTier.Basic,
        ProtoTenantTier.Standard => Models.TenantTier.Standard,
        ProtoTenantTier.Premium => Models.TenantTier.Premium,
        _ => Models.TenantTier.Basic
    };
}
