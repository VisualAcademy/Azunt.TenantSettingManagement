using Microsoft.EntityFrameworkCore;

namespace Azunt.TenantSettingManagement;

/// <summary>
/// EF Core DbContext for TenantSettings (per-database KV).
/// </summary>
public class TenantSettingsDbContext : DbContext
{
    public TenantSettingsDbContext(DbContextOptions<TenantSettingsDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<TenantSetting>(e =>
        {
            e.ToTable("TenantSettings", "dbo");
            e.HasKey(x => new { x.TenantID, x.SettingKey });
            e.Property(x => x.SettingKey).HasMaxLength(100);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
