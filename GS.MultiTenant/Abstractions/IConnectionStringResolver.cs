using GS.MultiTenant.Models;

namespace GS.MultiTenant.Abstractions;

public interface IConnectionStringResolver
{
    bool UsesDedicatedDatabase(TenantModel? tenant);

    string ResolveDedicated(TenantModel tenant);

    string ResolveShared();
}
