using System.Threading;
using System.Threading.Tasks;

namespace Azunt.TenantSettingManagement;

public interface ITenantSettingsService
{
    Task<string?> GetValueAsync(long tenantId, string key, CancellationToken ct = default);
    Task<bool> GetBoolAsync(long tenantId, string key, bool defaultValue = false, CancellationToken ct = default);
    void Invalidate(long tenantId, string key);
}
