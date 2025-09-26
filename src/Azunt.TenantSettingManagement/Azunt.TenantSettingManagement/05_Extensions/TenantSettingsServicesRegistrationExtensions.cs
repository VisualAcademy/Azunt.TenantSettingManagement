using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Azunt.TenantSettingManagement;

/// <summary>
/// DI helpers for TenantSettings module.
/// </summary>
public static class TenantSettingsServicesRegistrationExtensions
{
    public static IServiceCollection AddTenantSettingsModule(
        this IServiceCollection services,
        string connectionString,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Scoped)
    {
        services.AddMemoryCache();

        services.AddDbContext<TenantSettingsDbContext>(
            options => options.UseSqlServer(connectionString),
            dbContextLifetime);

        services.AddTransient<TenantSettingsDbContextFactory>();
        services.AddScoped<ITenantSettingsService, TenantSettingsService>();
        services.AddScoped<ITenantSettingsRepository, TenantSettingsRepository>();
        services.AddScoped<TenantSettingsAdminService>();

        return services;
    }

    public static IServiceCollection AddTenantSettingsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Scoped)
    {
        var cs = configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        return services.AddTenantSettingsModule(cs, dbContextLifetime);
    }
}
