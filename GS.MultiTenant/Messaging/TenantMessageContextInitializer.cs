using GS.MultiTenant.Messaging;

namespace GS.MultiTenant.Messaging;

public static class TenantMessageContextInitializer
{
    public static IDisposable? InitializeFromHeaders(IReadOnlyDictionary<string, object>? headers)
    {
        if (headers is null)
        {
            return null;
        }

        foreach (var key in new[] { TenantMessageHeaders.TenantId, "tenant_id", "TenantId" })
        {
            if (!headers.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            var tenantId = value switch
            {
                string s => s,
                byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
                _ => value.ToString()
            };

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return TenantMessageContext.SetTenant(tenantId);
            }
        }

        return null;
    }
}
