using Finbuckle.MultiTenant.Abstractions;
using GS.MultiTenant.Models;

namespace GS.MultiTenant.Services;

internal sealed class CurrentTenantAccessor : Abstractions.ICurrentTenantAccessor
{
    private readonly IMultiTenantContextAccessor<TenantModel> _contextAccessor;

    public CurrentTenantAccessor(IMultiTenantContextAccessor<TenantModel> contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public string? TenantId =>
        Current?.Id ?? Current?.Identifier ?? Messaging.TenantMessageContext.TenantId;

    public string? TenantCode =>
        Current?.Identifier ?? Messaging.TenantMessageContext.TenantId;

    public TenantModel? Current => _contextAccessor.MultiTenantContext?.TenantInfo;

    public TenantTier? Tier => Current?.Tier;

    public bool IsResolved => !string.IsNullOrWhiteSpace(TenantId);
}
