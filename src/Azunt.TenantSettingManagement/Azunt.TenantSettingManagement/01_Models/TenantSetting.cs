namespace Azunt.TenantSettingManagement;

/// <summary>
/// Represents a single configuration entry for a specific tenant.
/// Each record is identified by TenantID + SettingKey, allowing
/// per-tenant key–value storage for flexible feature toggles,
/// preferences, or other runtime settings.
/// </summary>
public sealed class TenantSetting
{
    /// <summary>
    /// Foreign key reference to the tenant (dbo.Tenants.ID).
    /// Identifies which tenant this setting belongs to.
    /// </summary>
    public long TenantID { get; set; }

    /// <summary>
    /// The unique key of the setting (e.g. "EmployeeSummary:Enabled").
    /// Together with TenantID, forms a composite key.
    /// </summary>
    public string SettingKey { get; set; } = default!;

    /// <summary>
    /// The value for this setting (nullable).
    /// Stored as string for flexibility; interpret as bool/int/json as needed.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Timestamp of the last update.
    /// Useful for auditing and cache invalidation.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Identifier of the user/system that last updated this setting.
    /// Typically stores username, userId, or "System".
    /// </summary>
    public string? UpdatedBy { get; set; }
}
