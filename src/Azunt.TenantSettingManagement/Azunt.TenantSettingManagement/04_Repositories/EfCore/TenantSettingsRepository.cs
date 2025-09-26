using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Azunt.TenantSettingManagement;

public sealed class TenantSettingsRepository : ITenantSettingsRepository
{
    private readonly TenantSettingsDbContextFactory _factory;

    public TenantSettingsRepository(TenantSettingsDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<TenantSetting> AddAsync(TenantSetting entity, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        db.TenantSettings.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<TenantSetting?> GetAsync(long tenantId, string settingKey, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.TenantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantID == tenantId && x.SettingKey == settingKey, ct);
    }

    public async Task<IReadOnlyList<TenantSetting>> GetAllAsync(long tenantId, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var list = await db.TenantSettings
            .AsNoTracking()
            .Where(x => x.TenantID == tenantId)
            .OrderBy(x => x.SettingKey)
            .ToListAsync(ct);
        return list;
    }

    public async Task<IReadOnlyList<TenantSetting>> SearchByPrefixAsync(long tenantId, string keyPrefix, int pageIndex = 0, int pageSize = 50, CancellationToken ct = default)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 50;

        await using var db = _factory.CreateDbContext();
        var q = db.TenantSettings
            .AsNoTracking()
            .Where(x => x.TenantID == tenantId && x.SettingKey.StartsWith(keyPrefix));

        var list = await q
            .OrderBy(x => x.SettingKey)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return list;
    }

    public async Task<bool> UpdateAsync(TenantSetting entity, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

        var existing = await db.TenantSettings
            .FirstOrDefaultAsync(x => x.TenantID == entity.TenantID && x.SettingKey == entity.SettingKey, ct);

        if (existing is null) return false;

        existing.Value = entity.Value;
        existing.UpdatedBy = entity.UpdatedBy;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        return await db.SaveChangesAsync(ct) > 0;
    }

    public async Task<TenantSetting> UpsertAsync(long tenantId, string settingKey, string? value, string? updatedBy = "System", CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

        var entity = await db.TenantSettings
            .FirstOrDefaultAsync(x => x.TenantID == tenantId && x.SettingKey == settingKey, ct);

        if (entity is null)
        {
            entity = new TenantSetting
            {
                TenantID = tenantId,
                SettingKey = settingKey,
                Value = value,
                UpdatedBy = updatedBy,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.TenantSettings.Add(entity);
        }
        else
        {
            entity.Value = value;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(long tenantId, string settingKey, CancellationToken ct = default)
    {
        await using var db = _factory.CreateDbContext();
        var entity = await db.TenantSettings
            .FirstOrDefaultAsync(x => x.TenantID == tenantId && x.SettingKey == settingKey, ct);

        if (entity is null) return false;

        db.TenantSettings.Remove(entity);
        return await db.SaveChangesAsync(ct) > 0;
    }
}
