using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.TenantSettingManagement;

/// <summary>
/// Factory for creating TenantSettingsDbContext instances.
/// </summary>
public class TenantSettingsDbContextFactory
{
    private readonly IConfiguration? _configuration;

    public TenantSettingsDbContextFactory() { }

    public TenantSettingsDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TenantSettingsDbContext CreateDbContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be null or empty.", nameof(connectionString));

        var options = new DbContextOptionsBuilder<TenantSettingsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TenantSettingsDbContext(options);
    }

    public TenantSettingsDbContext CreateDbContext()
    {
        if (_configuration is null)
            throw new InvalidOperationException("Configuration is not provided.");

        var cs = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("DefaultConnection is not configured properly.");

        return CreateDbContext(cs);
    }
}
