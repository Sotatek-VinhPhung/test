# 🐘 PostgreSQL Migration Guide

## ✅ Completed Steps

### 1. Connection String Updated
**File**: `src/CleanArchitecture.Api/appsettings.json`

```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;"
}
```

### 2. Database Provider Changed
**File**: `src/CleanArchitecture.Infrastructure/DependencyInjection.cs`

```csharp
// Changed from:
options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))

// To:
options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
```

### 3. Migration Created
**File**: `src/CleanArchitecture.Infrastructure/Migrations/20260414072821_InitialCreatePostgres.cs`

- ✅ Migration for PostgreSQL generated
- ✅ NuGet Package: `Npgsql.EntityFrameworkCore.PostgreSQL` (v8.0.*) already installed

### 4. Build Status
✅ **Build Successful** (0 errors)

---

## 🚀 Next Steps - Apply Migration to PostgreSQL

### Step 1: Ensure PostgreSQL is Running
```powershell
# Check if PostgreSQL is running
# On Windows, PostgreSQL should be in Services
# Or start via Docker:
docker run --name postgres-rbac -e POSTGRES_PASSWORD=123456 -p 5432:5432 -d postgres:latest
```

### Step 2: Create Database (if not exists)
```sql
-- Connect to PostgreSQL as admin
CREATE DATABASE rbac_db;
```

### Step 3: Apply Migration
```powershell
cd C:\test

# Apply the migration to PostgreSQL
dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api

# Output should show:
# Applying migration '20260414072821_InitialCreatePostgres'.
# Done.
```

### Step 4: Verify Tables Created
```sql
-- Connect to rbac_db and check tables
\dt

-- Should see these tables:
-- - users
-- - role_permissions (legacy)
-- - user_permission_overrides (legacy)
-- - roles (new RBAC)
-- - user_roles (new RBAC)
-- - subsystems (new RBAC)
-- - role_subsystem_permissions (new RBAC)
```

---

## 📊 Database Schema

### RBAC Tables (PostgreSQL)

#### 1. `subsystems` Table
```sql
CREATE TABLE subsystems (
    id UUID PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NULL
);
```

#### 2. `roles` Table
```sql
CREATE TABLE roles (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    is_system_role BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NULL
);
```

#### 3. `user_roles` Table
```sql
CREATE TABLE user_roles (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    assigned_at TIMESTAMP NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    UNIQUE (user_id, role_id)
);
```

#### 4. `role_subsystem_permissions` Table
```sql
CREATE TABLE role_subsystem_permissions (
    id UUID PRIMARY KEY,
    role_id UUID NOT NULL,
    subsystem_id UUID NOT NULL,
    permissions BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NULL,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    FOREIGN KEY (subsystem_id) REFERENCES subsystems(id) ON DELETE CASCADE,
    UNIQUE (role_id, subsystem_id)
);
```

#### 5. `user_permission_overrides` Table
```sql
CREATE TABLE user_permission_overrides (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    subsystem_id UUID NOT NULL,
    permissions BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (subsystem_id) REFERENCES subsystems(id) ON DELETE CASCADE,
    UNIQUE (user_id, subsystem_id)
);
```

---

## ✅ Connection Verification

### Test Connection from Application

1. **Start the API**:
```powershell
cd C:\test
dotnet run --project src\CleanArchitecture.Api
```

2. **Expected Output**:
```
info: Microsoft.EntityFrameworkCore.Infrastructure[10403]
      Entity Framework Core {version} initialized 'AppDbContext'
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

3. **Test with Swagger**:
   - Navigate to: `http://localhost:5000/swagger`
   - Try any endpoint that queries database
   - Should work without connection errors

### Connection String Format

```
Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;
```

**Components**:
- `Server`: `localhost` (PostgreSQL host)
- `Port`: `5432` (default PostgreSQL port)
- `Database`: `rbac_db` (your database name)
- `User Id`: `postgres` (username)
- `Password`: `123456` (your password)

---

## 🔄 Environment-Specific Configurations

### Development (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;"
  }
}
```

### Production (appsettings.Production.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db-host;Port=5432;Database=rbac_db_prod;User Id=prod_user;Password=***secure-password***;"
  }
}
```

### Staging (appsettings.Staging.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=staging-db-host;Port=5432;Database=rbac_db_staging;User Id=staging_user;Password=***secure-password***;"
  }
}
```

---

## 🐛 Troubleshooting

### Issue 1: "Unable to connect to PostgreSQL"
```
Error: Unable to connect to endpoint
```

**Solution**:
```powershell
# Verify PostgreSQL is running
# Check connection string in appsettings.json
# Test with psql:
psql -h localhost -U postgres -d rbac_db
```

### Issue 2: "Database does not exist"
```
Error: FATAL: database "rbac_db" does not exist
```

**Solution**:
```sql
-- Create database as postgres user
CREATE DATABASE rbac_db OWNER postgres;
```

### Issue 3: "Password authentication failed"
```
Error: password authentication failed for user "postgres"
```

**Solution**:
```powershell
# Update connection string with correct password
# Verify PostgreSQL user password
# Reset password if needed:
# ALTER USER postgres WITH PASSWORD 'new_password';
```

### Issue 4: Migration fails
```
Error: Failed to apply migration
```

**Solution**:
```powershell
# Check if database exists and is accessible
psql -h localhost -U postgres -d rbac_db

# Try migration again with verbose output
dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api --verbose
```

---

## 📝 Updated Configuration Files

### `appsettings.json` (UPDATED)
✅ PostgreSQL connection string configured

### `DependencyInjection.cs` (UPDATED)
✅ Changed from `UseSqlite()` to `UseNpgsql()`

### `InfrastructureProject.csproj`
✅ `Npgsql.EntityFrameworkCore.PostgreSQL` (v8.0.*) already present

---

## 🔍 Verification Checklist

- [x] Connection string updated to PostgreSQL
- [x] DbContext configured for PostgreSQL (`UseNpgsql()`)
- [x] EF Core PostgreSQL provider installed
- [x] Migration created (`InitialCreatePostgres`)
- [x] Build successful (0 errors)
- [ ] PostgreSQL running
- [ ] Database created (`rbac_db`)
- [ ] Migration applied (`dotnet ef database update`)
- [ ] Tables created in PostgreSQL
- [ ] API tested with database

---

## 🎯 Quick Commands Reference

```powershell
# Create fresh migration
dotnet ef migrations add MigrationName -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api

# Apply migration to database
dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api

# Revert last migration
dotnet ef migrations remove -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api --force

# Revert to specific migration
dotnet ef database update PreviousMigrationName -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api

# Show migration history
dotnet ef migrations list -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api

# Generate SQL script
dotnet ef migrations script -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api -o migration.sql
```

---

## 📚 Resources

- [Entity Framework Core - PostgreSQL](https://learn.microsoft.com/en-us/ef/core/providers/postgresql/)
- [Npgsql Documentation](https://www.npgsql.org/)
- [PostgreSQL Connection Strings](https://www.postgresql.org/docs/current/libpq-connect-using-login-files.html)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

## ✨ Summary

✅ **SQLite → PostgreSQL Migration Complete**

**Changes Made**:
1. Updated connection string in `appsettings.json`
2. Changed DbContext configuration to use PostgreSQL provider
3. Created new EF Core migration for PostgreSQL
4. Build verification: ✅ Success (0 errors)

**Next Action**: Apply migration to PostgreSQL database and test endpoints
