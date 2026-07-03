using GS.Core.Ambient;

namespace GS.MultiTenant.Services;

internal sealed class TenantBypassService : Abstractions.ITenantBypassService
{
    private static readonly AmbientContext<bool> Bypass = new();

    public bool IsBypassEnabled => Bypass.Value;

    public IDisposable EnableBypass() => Bypass.Set(true);
}
