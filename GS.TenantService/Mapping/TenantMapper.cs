using GS.MultiTenant.Models;

namespace GS.TenantService.Mapping;

internal static class TenantMapper
{
    public static TenantModel ToModel(Data.TenantEntity entity) => new()
    {
        Id = entity.Id,
        Identifier = entity.TenantCode,
        TenantName = entity.TenantName,
        Tier = entity.Tier,
        UsesDedicatedDatabase = entity.UsesDedicatedDatabase,
        DatabaseHost = entity.DatabaseHost,
        DatabasePort = entity.DatabasePort,
        CredentialsRef = entity.CredentialsRef
    };
}
