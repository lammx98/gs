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

        ValidateDatabaseConfig(
            request.UsesDedicatedDatabase,
            request.DatabaseHost,
            request.DatabasePort,
            request.CredentialsRef);

        var entity = new TenantEntity
        {
            Id = Guid.NewGuid(),
            TenantCode = tenantCode,
            TenantName = request.TenantName.Trim(),
            Tier = request.Tier,
            UsesDedicatedDatabase = request.UsesDedicatedDatabase,
            DatabaseHost = NormalizeOptional(request.DatabaseHost),
            DatabasePort = request.DatabasePort,
            CredentialsRef = NormalizeOptional(request.CredentialsRef),
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

        ValidateDatabaseConfig(
            request.UsesDedicatedDatabase,
            request.DatabaseHost,
            request.DatabasePort,
            request.CredentialsRef);

        entity.TenantName = request.TenantName.Trim();
        entity.Tier = request.Tier;
        entity.UsesDedicatedDatabase = request.UsesDedicatedDatabase;
        entity.DatabaseHost = NormalizeOptional(request.DatabaseHost);
        entity.DatabasePort = request.DatabasePort;
        entity.CredentialsRef = NormalizeOptional(request.CredentialsRef);
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateDatabaseConfig(
        bool usesDedicatedDatabase,
        string? databaseHost,
        int? databasePort,
        string? credentialsRef)
    {
        if (usesDedicatedDatabase)
        {
            if (string.IsNullOrWhiteSpace(databaseHost))
            {
                throw new HttpStatusException("Dedicated database requires DatabaseHost.", 400);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(databaseHost)
            || databasePort.HasValue
            || !string.IsNullOrWhiteSpace(credentialsRef))
        {
            throw new HttpStatusException(
                "Shared database tenants must not include dedicated database configuration.",
                400);
        }
    }
}
