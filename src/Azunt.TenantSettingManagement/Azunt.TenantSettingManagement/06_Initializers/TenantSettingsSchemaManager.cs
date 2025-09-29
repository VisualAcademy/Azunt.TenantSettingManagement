using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azunt.TenantSettingManagement;

public sealed class TenantSettingsSchemaManager
{
    private readonly ILogger<TenantSettingsSchemaManager> _logger;

    public TenantSettingsSchemaManager(ILogger<TenantSettingsSchemaManager> logger)
    {
        _logger = logger;
    }

    public void EnsureSchema(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        bool tableExists;
        using (var cmdCheck = new SqlCommand(@"
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TenantSettings'", connection))
        {
            tableExists = (int)cmdCheck.ExecuteScalar() > 0;
        }

        if (!tableExists)
        {
            using var cmdCreate = new SqlCommand(@"
CREATE TABLE [dbo].[TenantSettings](
    [TenantID]   BIGINT             NOT NULL,
    [SettingKey] NVARCHAR(100)      NOT NULL,
    [Value]      NVARCHAR(MAX)      NULL,
    [UpdatedAt]  DATETIMEOFFSET(7)  NOT NULL 
        CONSTRAINT DF_TenantSettings_UpdatedAt DEFAULT (SYSUTCDATETIME() AT TIME ZONE 'UTC'),
    [UpdatedBy]  NVARCHAR(100)      NULL,
    CONSTRAINT PK_TenantSettings PRIMARY KEY CLUSTERED (TenantID, SettingKey)
);", connection);
            cmdCreate.ExecuteNonQuery();
            _logger.LogInformation("Created dbo.TenantSettings");
        }
        else
        {
            EnsureUpdatedAtIsDateTimeOffset(connection);
        }

        using var cmdIndex = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TenantSettings_SettingKey' AND object_id = OBJECT_ID('dbo.TenantSettings'))
    CREATE NONCLUSTERED INDEX IX_TenantSettings_SettingKey ON dbo.TenantSettings(SettingKey, TenantID);", connection);
        cmdIndex.ExecuteNonQuery();
    }

    public void EnsureUpdatedAtIsDateTimeOffset(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        EnsureUpdatedAtIsDateTimeOffset(connection);
    }

    private void EnsureUpdatedAtIsDateTimeOffset(SqlConnection connection)
    {
        // 테이블 유무 확인
        using (var cmd = new SqlCommand("SELECT OBJECT_ID('dbo.TenantSettings','U')", connection))
        {
            var id = cmd.ExecuteScalar() as int?;
            if (id == null || id == 0) return; // 테이블 없음 → 스킵
        }

        // 현재 컬럼 타입 확인
        string? currentType;
        using (var cmdType = new SqlCommand(@"
SELECT t.name
FROM sys.columns c
JOIN sys.types   t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.TenantSettings') AND c.name = 'UpdatedAt';", connection))
        {
            currentType = cmdType.ExecuteScalar() as string;
        }

        if (string.Equals(currentType, "datetimeoffset", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("TenantSettings.UpdatedAt already DATETIMEOFFSET");
            return; // 이미 목표 타입
        }

        if (!string.Equals(currentType, "datetime2", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("TenantSettings.UpdatedAt unexpected type: {Type}. Skipping upgrade.", currentType);
            return;
        }

        // 업그레이드 배치(트랜잭션 + 기본값 제약 처리 + 안전 복사)
        var sql = @"
BEGIN TRY
    BEGIN TRAN;

    -- 1) 기존 DEFAULT 제약 제거
    DECLARE @dfName sysname;
    SELECT @dfName = d.name
    FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('dbo.TenantSettings')
      AND c.name = 'UpdatedAt';

    IF @dfName IS NOT NULL
        EXEC('ALTER TABLE dbo.TenantSettings DROP CONSTRAINT ' + QUOTENAME(@dfName) + ';');

    -- 2) 임시 컬럼 추가 (datetimeoffset(3))
    ALTER TABLE dbo.TenantSettings
    ADD UpdatedAt_tmp DATETIMEOFFSET(7) NULL;

    -- 3) 기존 값 UTC 오프셋으로 백필
    UPDATE dbo.TenantSettings
       SET UpdatedAt_tmp = CASE 
                               WHEN UpdatedAt IS NULL THEN NULL
                               ELSE TODATETIMEOFFSET(UpdatedAt, '+00:00')
                           END;

    -- 4) NOT NULL 적용
    ALTER TABLE dbo.TenantSettings
    ALTER COLUMN UpdatedAt_tmp DATETIMEOFFSET(7) NOT NULL;

    -- 5) 원 컬럼 삭제
    ALTER TABLE dbo.TenantSettings DROP COLUMN UpdatedAt;

    -- 6) 임시 컬럼 이름 변경
    EXEC sp_rename 'dbo.TenantSettings.UpdatedAt_tmp', 'UpdatedAt', 'COLUMN';

    -- 7) 새 DEFAULT 제약 (UTC 오프셋)
    ALTER TABLE dbo.TenantSettings
    ADD CONSTRAINT DF_TenantSettings_UpdatedAt
        DEFAULT (SYSUTCDATETIME() AT TIME ZONE 'UTC') FOR UpdatedAt;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH";

        using var cmdUpgrade = new SqlCommand(sql, connection);
        cmdUpgrade.ExecuteNonQuery();
        _logger.LogInformation("Upgraded TenantSettings.UpdatedAt to DATETIMEOFFSET(7)");
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
    VALUES (@TenantID, @SettingKey, @Value, SYSUTCDATETIME() AT TIME ZONE 'UTC', @UpdatedBy);", connection);

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
    VALUES (@TenantID, @SettingKey, @Value, SYSUTCDATETIME() AT TIME ZONE 'UTC', @UpdatedBy);", connection);

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
    VALUES (@TenantID, @SettingKey, @Value, SYSUTCDATETIME() AT TIME ZONE 'UTC', @UpdatedBy);", connection);

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
