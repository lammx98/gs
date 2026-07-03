using GS.Core.Ambient;

namespace GS.MultiTenant.Messaging;

/// <summary>
/// Ambient tenant context for message consumers and background workers.
/// </summary>
public static class TenantMessageContext
{
    private static readonly AmbientContext<string> Context = new();

    public static string? TenantId => Context.Value;

    public static IDisposable SetTenant(string tenantId) => Context.Set(tenantId);
}
