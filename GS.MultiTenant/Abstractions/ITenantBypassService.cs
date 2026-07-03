namespace GS.MultiTenant.Abstractions;

/// <summary>
/// Allows privileged jobs to bypass tenant query filters.
/// </summary>
public interface ITenantBypassService
{
    bool IsBypassEnabled { get; }

    IDisposable EnableBypass();
}
