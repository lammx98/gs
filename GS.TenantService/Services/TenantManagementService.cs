using GS.Core.Exceptions;
using GS.MultiTenant.Models;
using GS.TenantService.Contracts;
using GS.TenantService.Data;
using GS.TenantService.Mapping;
using Microsoft.EntityFrameworkCore;

namespace GS.TenantService.Services;

public sealed class TenantManagementService : ITenantManagementService
{
    private readonly TenantDbContext _dbContext;

    public TenantManagementService(TenantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TenantModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .OrderBy(x => x.TenantCode)
            .ToListAsync(cancellationToken);

        return tenants.Select(TenantMapper.ToModel).ToList();
    }

    public async Task<TenantModel?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(tenantCode);
        var entity = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantCode == normalized && x.IsActive, cancellationToken);

        return entity is null ? null : TenantMapper.ToModel(entity);
    }

    public async Task<TenantModel?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId && x.IsActive, cancellationToken);

        return entity is null ? null : TenantMapper.ToModel(entity);
    }

    public async Task<TenantModel> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenantCode = NormalizeCode(request.TenantCode);

        if (await _dbContext.Tenants.AnyAsync(x => x.TenantCode == tenantCode, cancellationToken))
        {
            throw new HttpStatusException($"Tenant code '{tenantCode}' already exists.", 409);
        }

        ValidateTierConnectionString(request.Tier, request.ConnectionString);

        var entity = new TenantEntity
        {
            Id = Guid.NewGuid(),
            TenantCode = tenantCode,
            TenantName = request.TenantName.Trim(),
            Tier = request.Tier,
            ConnectionString = string.IsNullOrWhiteSpace(request.ConnectionString) ? null : request.ConnectionString.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Tenants.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return TenantMapper.ToModel(entity);
    }

    public async Task<TenantModel?> UpdateAsync(string tenantCode, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(tenantCode);
        var entity = await _dbContext.Tenants.FirstOrDefaultAsync(x => x.TenantCode == normalized, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        ValidateTierConnectionString(request.Tier, request.ConnectionString);

        entity.TenantName = request.TenantName.Trim();
        entity.Tier = request.Tier;
        entity.ConnectionString = string.IsNullOrWhiteSpace(request.ConnectionString) ? null : request.ConnectionString.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return TenantMapper.ToModel(entity);
    }

    public async Task<bool> DeleteAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(tenantCode);
        var entity = await _dbContext.Tenants.FirstOrDefaultAsync(x => x.TenantCode == normalized, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeCode(string tenantCode) =>
        tenantCode.Trim().ToLowerInvariant();

    private static void ValidateTierConnectionString(TenantTier tier, string? connectionString)
    {
        if (tier == TenantTier.Vip && string.IsNullOrWhiteSpace(connectionString))
        {
            throw new HttpStatusException("VIP tier requires a dedicated ConnectionString.", 400);
        }
    }
}
