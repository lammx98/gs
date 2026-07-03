using Microsoft.EntityFrameworkCore;

namespace GS.TenantService.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantEntity>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TenantCode).IsUnique();
            entity.Property(x => x.TenantCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TenantName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ConnectionString).HasMaxLength(2048);
        });
    }
}
