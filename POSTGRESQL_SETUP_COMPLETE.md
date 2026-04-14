# 🐘 PostgreSQL Migration - Complete Setup Guide

## ✅ What's Been Done

### 1. **Connection String Updated** ✅
**File**: `src/CleanArchitecture.Api/appsettings.json`

Changed from SQLite to PostgreSQL:
```json
// FROM (SQLite)
"DefaultConnection": "Data Source=cleanarchitecture.db"

// TO (PostgreSQL)
"DefaultConnection": "Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;"
```

### 2. **Database Provider Changed** ✅
**File**: `src/CleanArchitecture.Infrastructure/DependencyInjection.cs`

```csharp
// FROM (SQLite)
options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))

// TO (PostgreSQL)
options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
```

### 3. **EF Core Migration Created** ✅
**File**: `src/CleanArchitecture.Infrastructure/Migrations/20260414072821_InitialCreatePostgres.cs`

- ✅ New migration for PostgreSQL generated
- ✅ All tables properly configured for PostgreSQL
- ✅ UUID types mapped correctly
- ✅ Timestamps configured for PostgreSQL

### 4. **Build Verified** ✅
```
Build successful (0 errors, 0 warnings)
```

### 5. **NuGet Package Already Installed** ✅
```
Npgsql.EntityFrameworkCore.PostgreSQL v8.0.*
```

---

## 🚀 Your Next Steps (Do This Now)

### Step 1: Verify PostgreSQL is Running

**Option A: Docker** (Easiest)
```powershell
# If PostgreSQL not running, start with Docker
docker run --name postgres-rbac `
  -e POSTGRES_PASSWORD=123456 `
  -p 5432:5432 `
  -d postgres:latest
```

**Option B: Local Installation**
- PostgreSQL should be running on `localhost:5432`
- Check Services or use command: `pg_isready -h localhost`

### Step 2: Create Database

```powershell
# Method 1: Using PowerShell script
.\setup-postgres.ps1

# Method 2: Manual SQL
# Open pgAdmin or psql and run:
# CREATE DATABASE rbac_db;
```

### Step 3: Apply Migration

```powershell
cd C:\test

# Apply the migration to PostgreSQL
dotnet ef database update `
  -p src\CleanArchitecture.Infrastructure `
  -s src\CleanArchitecture.Api

# Expected output:
# Applying migration '20260414072821_InitialCreatePostgres'.
# Done.
```

### Step 4: Verify Tables Created

```powershell
# Connect to PostgreSQL and verify
psql -h localhost -U postgres -d rbac_db -c "\dt"

# Should see these 7 tables:
# - AspNetRoles (if Identity enabled)
# - AspNetUsers (if Identity enabled)
# - RolePermissions (legacy)
# - Roles (new RBAC)
# - RoleSubsystemPermissions
# - Subsystems
# - UserPermissionOverrides
# - UserRoles
# - Users
```

### Step 5: Run the Application

```powershell
cd C:\test
dotnet run --project src\CleanArchitecture.Api

# Should show:
# Now listening on: http://localhost:5000
# Application started. Press Ctrl+C to shut down.
```

### Step 6: Test with Swagger

1. Open browser: `http://localhost:5000/swagger`
2. Try any API endpoint (e.g., `GET /api/permissions/me`)
3. Should work without database errors

---

## 📊 Database Schema Summary

### Tables Created by Migration

| Table | Purpose | Rows |
|-------|---------|------|
| `Roles` | System roles (Admin, Manager, User, etc.) | 5-10 |
| `Subsystems` | Permission subsystems (Users, Reports, Analytics) | 5+ |
| `UserRoles` | User-Role assignments | Dynamic |
| `RoleSubsystemPermissions` | Role-Subsystem-Permission mapping | Dynamic |
| `UserPermissionOverrides` | User-level permission overrides | Dynamic |
| `Users` | User accounts | Dynamic |
| `RolePermissions` (Legacy) | Old permission structure | Legacy |

---

## 🔐 Connection String Format

```
Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;
```

**Components**:
- `Server`: PostgreSQL hostname
- `Port`: PostgreSQL port (default 5432)
- `Database`: Database name (`rbac_db`)
- `User Id`: PostgreSQL username (`postgres`)
- `Password`: PostgreSQL password (`123456`)

---

## 🎯 Configuration Files Changed

### `appsettings.json` ✅
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;"
  },
  // ... other settings
}
```

### `DependencyInjection.cs` ✅
```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
```

---

## 🛠️ Useful Commands

### Apply Migration
```powershell
dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api
```

### Revert Migration
```powershell
dotnet ef database update "PreviousMigrationName" -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api
```

### View Migration History
```powershell
dotnet ef migrations list -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api
```

### Generate SQL Script
```powershell
dotnet ef migrations script -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api -o migration.sql
```

### Create New Migration
```powershell
dotnet ef migrations add MigrationName -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api
```

---

## ✅ Pre-Flight Checklist

Before running the application:

- [ ] PostgreSQL is installed and running
- [ ] Connection string in `appsettings.json` is correct
- [ ] Database `rbac_db` exists
- [ ] Migration applied successfully
- [ ] Tables visible in PostgreSQL
- [ ] Build succeeds (0 errors)
- [ ] API can connect to database

---

## 🐛 Troubleshooting

### Error: "Unable to connect to endpoint"
```powershell
# Verify PostgreSQL is running
pg_isready -h localhost -p 5432

# If not running:
# 1. Windows: Start PostgreSQL from Services
# 2. Docker: docker start postgres-rbac
```

### Error: "Database does not exist"
```sql
-- Create database
CREATE DATABASE rbac_db OWNER postgres;
```

### Error: "Authentication failed"
```powershell
# Check password in appsettings.json
# Reset PostgreSQL password if needed:
# ALTER USER postgres WITH PASSWORD 'new_password';
```

### Error: "Migration failed"
```powershell
# Run with verbose output
dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api --verbose

# Check migration file syntax
# Check database permissions
```

### Error: "Duplicate key value violates unique constraint"
```powershell
# Database already has data from previous migration
# Drop and recreate:
psql -h localhost -U postgres -d rbac_db -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
# Then reapply migration
```

---

## 📈 Environment-Specific Configurations

### Local Development
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;"
  }
}
```

### Staging
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=staging-db.example.com;Port=5432;Database=rbac_staging;User Id=staging_user;Password=***;"
  }
}
```

### Production
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db.example.com;Port=5432;Database=rbac_prod;User Id=prod_user;Password=***;"
  }
}
```

---

## 📚 Setup Scripts Available

### Windows PowerShell Script
```powershell
# Run setup script
.\setup-postgres.ps1

# This will:
# 1. Check PostgreSQL installation
# 2. Test database connection
# 3. Create database if needed
# 4. Display connection string
# 5. Show next steps
```

### Linux/Mac Bash Script
```bash
# Make script executable
chmod +x setup-postgres.sh

# Run setup script
./setup-postgres.sh
```

---

## ✨ Summary

| Component | Status | Details |
|-----------|--------|---------|
| **Connection String** | ✅ Updated | PostgreSQL configured |
| **DbContext** | ✅ Updated | Using `UseNpgsql()` |
| **EF Core Package** | ✅ Installed | Npgsql.EntityFrameworkCore.PostgreSQL v8.0 |
| **Migration** | ✅ Created | `InitialCreatePostgres` |
| **Build** | ✅ Success | 0 errors |
| **Database** | ⏳ Pending | Need to create and apply migration |
| **API Testing** | ⏳ Pending | After migration applied |

---

## 🎯 Next Action

**Right now**, run this command:

```powershell
cd C:\test
dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api
```

Then test:
```powershell
dotnet run --project src\CleanArchitecture.Api
```

Visit: `http://localhost:5000/swagger`

✅ **Done!** Your app is now using PostgreSQL instead of SQLite.

---

## 📞 Need Help?

1. Check `POSTGRESQL_MIGRATION_GUIDE.md` for detailed information
2. Review error messages in the console
3. Verify PostgreSQL is running: `pg_isready -h localhost`
4. Check connection string in `appsettings.json`
5. Review migration status: `dotnet ef migrations list`

---

**Last Updated**: 2024-04-14
**Status**: Ready for Migration Application
