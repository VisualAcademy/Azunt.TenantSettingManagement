using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azunt.TenantSettingManagement;

/// <summary>
/// High-level admin operations built on the repository + cache invalidation.
/// </summary>
public sealed class TenantSettingsAdminService
{
    private readonly ITenantSettingsRepository _repo;
    private readonly ITenantSettingsService _reader;

    public TenantSettingsAdminService(ITenantSettingsRepository repo, ITenantSettingsService reader)
    {
        _repo = repo;
        _reader = reader;
    }

    public async Task<TenantSetting> UpsertAsync(long tenantId, string key, string? value, string? updatedBy = "System", CancellationToken ct = default)
    {
        var saved = await _repo.UpsertAsync(tenantId, key, value, updatedBy, ct);
        _reader.Invalidate(tenantId, key);
        return saved;
    }

    public async Task<bool> DeleteAsync(long tenantId, string key, CancellationToken ct = default)
    {
        var ok = await _repo.DeleteAsync(tenantId, key, ct);
        if (ok) _reader.Invalidate(tenantId, key);
        return ok;
    }

    public Task<TenantSetting?> GetAsync(long tenantId, string key, CancellationToken ct = default)
        => _repo.GetAsync(tenantId, key, ct);

    public Task<IReadOnlyList<TenantSetting>> GetAllAsync(long tenantId, CancellationToken ct = default)
        => _repo.GetAllAsync(tenantId, ct);

    public Task<IReadOnlyList<TenantSetting>> SearchByPrefixAsync(long tenantId, string keyPrefix, int pageIndex = 0, int pageSize = 50, CancellationToken ct = default)
        => _repo.SearchByPrefixAsync(tenantId, keyPrefix, pageIndex, pageSize, ct);
}
