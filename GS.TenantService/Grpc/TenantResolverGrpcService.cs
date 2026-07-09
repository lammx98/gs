using Grpc.Core;
using GS.MultiTenant.Grpc.Tenant;
using GS.MultiTenant.Models;
using GS.TenantService.Services;

namespace GS.TenantService.Grpc;

public sealed class TenantResolverGrpcService : TenantResolver.TenantResolverBase
{
    private readonly ITenantManagementService _tenantService;

    public TenantResolverGrpcService(ITenantManagementService tenantService)
    {
        _tenantService = tenantService;
    }

    public override async Task<GetTenantResponse> GetByTenantCode(
        GetByTenantCodeRequest request,
        ServerCallContext context)
    {
        var tenant = await _tenantService.GetByTenantCodeAsync(request.TenantCode, context.CancellationToken);
        return ToResponse(tenant);
    }

    public override async Task<GetTenantResponse> GetByTenantId(
        GetByTenantIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.TenantId, out var tenantId))
        {
            return new GetTenantResponse { Found = false };
        }

        var tenant = await _tenantService.GetByTenantIdAsync(tenantId, context.CancellationToken);
        return ToResponse(tenant);
    }

    private static GetTenantResponse ToResponse(TenantModel? tenant)
    {
        if (tenant is null)
        {
            return new GetTenantResponse { Found = false };
        }

        return new GetTenantResponse
        {
            Found = true,
            Tenant = TenantGrpcMapping.ToMessage(tenant)
        };
    }
}
