# Azunt.TenantSettingManagement

Reusable .NET library for multi-tenant settings management with EF Core.

A lightweight, reusable **tenant settings** (key–value) library for .NET.
- **EF Core** (SQL Server)
- **IMemoryCache** for fast reads
- **No dependency** on your existing *Tenants* schema — pass a **connection string**, and use the initializers if you want to ensure the `dbo.TenantSettings` table exists.
