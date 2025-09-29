using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azunt.TenantSettingManagement;

/// <summary>
/// Ensures/augments TenantSettings schema in a single database and supports opt-in seeding.
/// No dependency on a Tenants table — caller supplies connectionString and tenant IDs for seeding.
/// </summary>
public sealed class TenantSettingsSchemaManager
{
    private readonly ILogger<TenantSettingsSchemaManager> _logger;

    public TenantSettingsSchemaManager(ILogger<TenantSettingsSchemaManager> logger)
    {
        _logger = logger;
    }

    /// <summary>Create dbo.TenantSettings if missing; add common index if missing.</summary>
    public void EnsureSchema(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using (var cmdCheck = new SqlCommand(@"
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TenantSettings'", connection))
        {
            var exists = (int)cmdCheck.ExecuteScalar() > 0;
            if (!exists)
            {
                using var cmdCreate = new SqlCommand(@"
CREATE TABLE [dbo].[TenantSettings](
    [TenantID]   BIGINT        NOT NULL,
    [SettingKey] NVARCHAR(100) NOT NULL,
    [Value]      NVARCHAR(MAX) NULL,
    [UpdatedAt]  DATETIME2(3)  NOT NULL CONSTRAINT DF_TenantSettings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    [UpdatedBy]  NVARCHAR(100) NULL,
    CONSTRAINT PK_TenantSettings PRIMARY KEY CLUSTERED (TenantID, SettingKey)
);", connection);
                cmdCreate.ExecuteNonQuery();
                _logger.LogInformation("Created dbo.TenantSettings");
            }
        }

        using var cmdIndex = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TenantSettings_SettingKey' AND object_id = OBJECT_ID('dbo.TenantSettings'))
    CREATE NONCLUSTERED INDEX IX_TenantSettings_SettingKey ON dbo.TenantSettings(SettingKey, TenantID);", connection);
        cmdIndex.ExecuteNonQuery();
    }

    /// <summary>
    /// Seed a key for a single tenant if missing.
    /// </summary>
    public void SeedIfMissing(string connectionString, long tenantId, string settingKey, string defaultValue, string? updatedBy = "System")
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM dbo.TenantSettings WHERE TenantID = @TenantID AND SettingKey = @SettingKey)
    INSERT INTO dbo.TenantSettings (TenantID, SettingKey, Value, UpdatedAt, UpdatedBy)
    VALUES (@TenantID, @SettingKey, @Value, SYSUTCDATETIME(), @UpdatedBy);", connection);

        cmd.Parameters.AddWithValue("@TenantID", tenantId);
        cmd.Parameters.AddWithValue("@SettingKey", settingKey);
        cmd.Parameters.AddWithValue("@Value", (object?)defaultValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Seed one key for multiple tenants in a single DB, skipping existing rows.
    /// </summary>
    public void SeedBulkIfMissing(string connectionString, IEnumerable<long> tenantIds, string settingKey, string defaultValue, string? updatedBy = "System")
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        foreach (var tenantId in tenantIds)
        {
            using var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM dbo.TenantSettings WHERE TenantID = @TenantID AND SettingKey = @SettingKey)
    INSERT INTO dbo.TenantSettings (TenantID, SettingKey, Value, UpdatedAt, UpdatedBy)
    VALUES (@TenantID, @SettingKey, @Value, SYSUTCDATETIME(), @UpdatedBy);", connection);

            cmd.Parameters.AddWithValue("@TenantID", tenantId);
            cmd.Parameters.AddWithValue("@SettingKey", settingKey);
            cmd.Parameters.AddWithValue("@Value", (object?)defaultValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Reads all Tenant IDs from dbo.Tenants and seeds the setting if missing for each tenant.
    /// </summary>
    public void SeedFromTenantsTable(string connectionString, string settingKey, string defaultValue, string? updatedBy = "System")
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // 1) Tenants.ID 목록 가져오기
        var tenantIds = new List<long>();
        using (var cmd = new SqlCommand("SELECT ID FROM dbo.Tenants", connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                tenantIds.Add(reader.GetInt64(0));
            }
        }

        // 2) 각 테넌트에 대해 Seed 실행
        foreach (var tenantId in tenantIds)
        {
            using var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM dbo.TenantSettings WHERE TenantID = @TenantID AND SettingKey = @SettingKey)
    INSERT INTO dbo.TenantSettings (TenantID, SettingKey, Value, UpdatedAt, UpdatedBy)
    VALUES (@TenantID, @SettingKey, @Value, SYSUTCDATETIME(), @UpdatedBy);", connection);

            cmd.Parameters.AddWithValue("@TenantID", tenantId);
            cmd.Parameters.AddWithValue("@SettingKey", settingKey);
            cmd.Parameters.AddWithValue("@Value", (object?)defaultValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);

            var rows = cmd.ExecuteNonQuery();
            if (rows > 0)
            {
                _logger.LogInformation("Seeded {SettingKey}={Value} for TenantID={TenantID}", settingKey, defaultValue, tenantId);
            }
        }
    }
}
