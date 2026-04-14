# 🐘 PostgreSQL Migration - Quick Reference Card

## ✅ Configuration Status

| Item | Status | Details |
|------|--------|---------|
| Connection String | ✅ | `Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;` |
| DbContext Provider | ✅ | `UseNpgsql()` |
| EF Core Package | ✅ | `Npgsql.EntityFrameworkCore.PostgreSQL` v8.0 |
| Migration Created | ✅ | `InitialCreatePostgres` |
| Build Status | ✅ | 0 errors |

---

## 🚀 Quick Start (3 Steps)

### 1️⃣ Ensure PostgreSQL Running
```powershell
# Check connection
psql -h localhost -U postgres

# Or start with Docker
docker run --name postgres-rbac -e POSTGRES_PASSWORD=123456 -p 5432:5432 -d postgres:latest
```

### 2️⃣ Create Database
```sql
CREATE DATABASE rbac_db;
```

### 3️⃣ Apply Migration & Run
```powershell
# Navigate to project
cd C:\test

# Apply migration
dotnet ef database update `
  -p src\CleanArchitecture.Infrastructure `
  -s src\CleanArchitecture.Api

# Run application
dotnet run --project src\CleanArchitecture.Api
```

✅ Done! Visit `http://localhost:5000/swagger`

---

## 📝 Files Modified

1. ✅ `src/CleanArchitecture.Api/appsettings.json`
   - Updated connection string to PostgreSQL

2. ✅ `src/CleanArchitecture.Infrastructure/DependencyInjection.cs`
   - Changed from `UseSqlite()` to `UseNpgsql()`

3. ✅ `src/CleanArchitecture.Infrastructure/Migrations/20260414072821_InitialCreatePostgres.cs`
   - New migration for PostgreSQL

---

## 🎯 Connection String Components

```
Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;
                   |          |          |            |                 |
                   |          |          |            |                 └─ Password
                   |          |          |            └─ Username
                   |          |          └─ Database name
                   |          └─ Port
                   └─ Host
```

---

## 🔧 Common Commands

| Command | Purpose |
|---------|---------|
| `dotnet ef database update` | Apply pending migrations |
| `dotnet ef migrations list` | Show all migrations |
| `dotnet ef migrations add MyMigration` | Create new migration |
| `dotnet ef migrations remove` | Remove last migration |
| `dotnet ef database update PreviousMigrationName` | Revert to specific migration |
| `pg_isready -h localhost` | Check PostgreSQL connection |
| `psql -h localhost -U postgres` | Connect to PostgreSQL |

---

## 📊 Database Tables

After migration, these tables exist:

- `Roles` - System roles
- `Subsystems` - Permission subsystems
- `UserRoles` - User-Role assignments
- `RoleSubsystemPermissions` - Role permissions
- `Users` - User accounts
- `UserPermissionOverrides` - User permission overrides
- `RolePermissions` - Legacy permissions

---

## ⚠️ If Something Goes Wrong

### Can't connect to PostgreSQL
```powershell
# Verify PostgreSQL is running
pg_isready -h localhost -p 5432

# If not, start it:
# Windows: Services → PostgreSQL → Start
# Docker: docker start postgres-rbac
# Or reinstall from postgres.org
```

### Database doesn't exist
```sql
CREATE DATABASE rbac_db OWNER postgres;
```

### Migration failed
```powershell
# Get detailed error message
dotnet ef database update `
  -p src\CleanArchitecture.Infrastructure `
  -s src\CleanArchitecture.Api `
  --verbose
```

### Port 5432 already in use
```powershell
# Check what's using the port
netstat -ano | findstr :5432

# Or use different port in connection string:
# Server=localhost;Port=5433;...
```

---

## 📚 Full Documentation

- **Detailed Guide**: `POSTGRESQL_MIGRATION_GUIDE.md`
- **Complete Setup**: `POSTGRESQL_SETUP_COMPLETE.md`
- **Setup Script**: `setup-postgres.ps1` (Windows)

---

## ✨ Summary

```
SQLite → PostgreSQL Migration Complete ✅

Changes:
  ✅ Connection string updated
  ✅ DbContext configured
  ✅ Migration created
  ✅ Build verified

Next: Apply migration and run application
```

---

**🎯 Your Connection String**:
```
Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;
```

**🚀 Next Command**:
```powershell
dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api
```
