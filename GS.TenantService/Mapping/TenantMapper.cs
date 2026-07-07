using GS.MultiTenant.Models;
using GS.TenantService.Data;

namespace GS.TenantService.Mapping;

internal static class TenantMapper
{
    public static TenantModel ToModel(TenantEntity entity) => new()
    {
        Id = entity.Id.ToString(),
        Identifier = entity.TenantCode,
        TenantName = entity.TenantName,
        Tier = entity.Tier,
        UsesDedicatedDatabase = entity.UsesDedicatedDatabase,
        DatabaseHost = entity.DatabaseHost,
        DatabasePort = entity.DatabasePort,
        CredentialsRef = entity.CredentialsRef
    };
}
