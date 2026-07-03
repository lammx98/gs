using GS.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GS.MultiTenant.Data;

public static class TenantQueryableExtensions
{
    public static IQueryable<T> ApplyTenantPolicy<T>(
        this IQueryable<T> source,
        ITenantBypassService bypassService)
        where T : class
    {
        return bypassService.IsBypassEnabled
            ? source.IgnoreQueryFilters()
            : source;
    }
}
