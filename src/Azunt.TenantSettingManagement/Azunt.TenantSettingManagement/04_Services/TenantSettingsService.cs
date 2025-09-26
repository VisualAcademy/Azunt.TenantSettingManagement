using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Azunt.TenantSettingManagement;

/// <summary>
/// Cached settings reader (per tenant).
/// </summary>
public sealed class TenantSettingsService : ITenantSettingsService
{
    private readonly TenantSettingsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    private static string CacheKey(long tenantId, string key) => $"TenantSettings:{tenantId}:{key}";

    public TenantSettingsService(TenantSettingsDbContextFactory factory, IMemoryCache cache)
    {
        _factory = factory;
        _cache = cache;
    }

    public async Task<string?> GetValueAsync(long tenantId, string key, CancellationToken ct = default)
    {
        var ck = CacheKey(tenantId, key);
        if (_cache.TryGetValue<string?>(ck, out var cached))
            return cached;

        await using var db = _factory.CreateDbContext();
        var val = await db.TenantSettings
            .AsNoTracking()
            .Where(x => x.TenantID == tenantId && x.SettingKey == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(ct);

        _cache.Set(ck, val, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
        return val;
    }

    public async Task<bool> GetBoolAsync(long tenantId, string key, bool defaultValue = false, CancellationToken ct = default)
    {
        var v = await GetValueAsync(tenantId, key, ct);
        if (string.IsNullOrWhiteSpace(v)) return defaultValue;
        var s = v.Trim();
        return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("1");
    }

    public void Invalidate(long tenantId, string key) => _cache.Remove(CacheKey(tenantId, key));
}
