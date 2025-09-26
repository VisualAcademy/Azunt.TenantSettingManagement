using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azunt.TenantSettingManagement;

public interface ITenantSettingsRepository
{
    // Create
    Task<TenantSetting> AddAsync(TenantSetting entity, CancellationToken ct = default);

    // Read
    Task<TenantSetting?> GetAsync(long tenantId, string settingKey, CancellationToken ct = default);
    Task<IReadOnlyList<TenantSetting>> GetAllAsync(long tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantSetting>> SearchByPrefixAsync(long tenantId, string keyPrefix, int pageIndex = 0, int pageSize = 50, CancellationToken ct = default);

    // Update
    Task<bool> UpdateAsync(TenantSetting entity, CancellationToken ct = default);

    // Upsert (insert or update by key)
    Task<TenantSetting> UpsertAsync(long tenantId, string settingKey, string? value, string? updatedBy = "System", CancellationToken ct = default);

    // Delete
    Task<bool> DeleteAsync(long tenantId, string settingKey, CancellationToken ct = default);
}
