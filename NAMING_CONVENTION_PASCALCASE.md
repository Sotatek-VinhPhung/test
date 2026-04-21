# 📋 BẢNG NAMING CONVENTION - PASCALCASE

## ✅ Cập nhật Hoàn Thành

Tất cả bảng PostgreSQL giờ đã **viết hoa PascalCase** như C# DbSet:

| C# DbSet | DB Bảng | Trường | 
|----------|---------|--------|
| `Users` | `"Users"` | `"Id"`, `"FirstName"`, `"IsActive"`, `"CreatedAt"` |
| `Roles` | `"Roles"` | `"Id"`, `"Code"`, `"Name"` |
| `Subsystems` | `"Subsystems"` | `"Id"`, `"Code"`, `"Name"` |
| `UserRoles` | `"UserRoles"` | `"UserId"`, `"RoleId"` |
| `RoleSubsystemPermissions` | `"RoleSubsystemPermissions"` | `"RoleId"`, `"SubsystemId"`, `"Flags"` |
| `UserPermissionOverrides` | `"UserPermissionOverrides"` | `"UserId"`, `"Module"`, `"Flags"` |
| `Regions` | `"Regions"` | `"Id"`, `"Code"`, `"Name"` |
| `Companies` | `"Companies"` | `"Id"`, `"Code"`, `"RegionId"` |
| `Departments` | `"Departments"` | `"Id"`, `"Code"`, `"CompanyId"` |
| `RoleOrganizationScopes` | `"RoleOrganizationScopes"` | `"RoleId"`, `"RegionId"`, `"CompanyId"` |

## 📝 Những Gì Đã Thay Đổi

### 1. SQL Script: `database/create_rbac_schema.sql`
✅ Tất cả bảng + trường → **PascalCase**
- Bảng: `"Users"`, `"Roles"`, `"Subsystems"`, ...
- Trường: `"Id"`, `"FirstName"`, `"IsActive"`, ...

### 2. EF Core Configurations
✅ Tất cả files configuration cập nhật:

```csharp
// UserConfiguration.cs
builder.ToTable("Users"); // Chỉ định bảng PascalCase

// RoleConfiguration.cs
builder.ToTable("Roles");

// SubsystemConfiguration.cs
builder.ToTable("Subsystems");

// UserRoleConfiguration.cs
builder.ToTable("UserRoles");

// RoleSubsystemPermissionConfiguration.cs
builder.ToTable("RoleSubsystemPermissions");

// UserPermissionOverrideConfiguration.cs
builder.ToTable("UserPermissionOverrides");

// HierarchicalOrganizationConfigurations.cs
builder.ToTable("Regions");
builder.ToTable("Companies");
builder.ToTable("Departments");
builder.ToTable("RoleOrganizationScopes");
```

## 🔧 Cách EF Core Mapping

### Before (Implicit - Auto snake_case):
```csharp
public DbSet<User> Users => Set<User>();
// → PostgreSQL: "users" (tự động)
```

### After (Explicit - PascalCase):
```csharp
public DbSet<User> Users => Set<User>();

// Trong UserConfiguration:
builder.ToTable("Users"); // → PostgreSQL: "Users"
```

## 🚀 Cách Sử Dụng

### 1. Chạy SQL Script trong DBeaver
```sql
-- File: database/create_rbac_schema.sql
-- Mở trong DBeaver
-- Select All (Ctrl+A)
-- Execute (Ctrl+Enter)
```

### 2. Verify Bảng
```sql
-- Query này sẽ show tất cả bảng
SELECT tablename FROM pg_tables 
WHERE schemaname = 'public';

-- Output:
-- Users
-- Roles
-- Subsystems
-- UserRoles
-- RoleSubsystemPermissions
-- UserPermissionOverrides
-- Regions
-- Companies
-- Departments
-- RoleOrganizationScopes
```

### 3. Verify Columns
```sql
-- Kiểm tra các trường trong bảng Users
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Users';

-- Output:
-- Id | uuid
-- FirstName | character varying
-- LastName | character varying
-- Email | character varying
-- PasswordHash | character varying
-- IsActive | boolean
-- CreatedAt | timestamp with time zone
-- RegionId | uuid
-- CompanyId | uuid
-- DepartmentId | uuid
```

## 📊 Bảng Tóm Tắt Convention

| Aspect | Convention | Ví Dụ |
|--------|-----------|--------|
| **C# DbSet** | PascalCase | `Users`, `Roles`, `UserRoles` |
| **C# Property** | PascalCase | `FirstName`, `IsActive`, `CreatedAt` |
| **PostgreSQL Bảng** | PascalCase | `"Users"`, `"Roles"`, `"UserRoles"` |
| **PostgreSQL Trường** | PascalCase | `"FirstName"`, `"IsActive"`, `"CreatedAt"` |
| **Foreign Keys** | `[Table]Id` | `UserId`, `RoleId`, `RegionId` |
| **Indexes** | Descriptive | `idx_users_email`, `idx_roles_code` |

## ✨ Benefits

✅ **Consistent naming**: C# ↔ PostgreSQL đều PascalCase  
✅ **Easy to read**: Tên bảng giống DbSet  
✅ **Less confusion**: Không cần convert từ PascalCase → snake_case  
✅ **Explicit mapping**: Dễ debug bằng cách xem configuration  

## 🎯 Build Status

✅ **Build: SUCCESS** (0 errors)  
✅ **Configurations: Updated** (9 files)  
✅ **SQL Script: PascalCase** (all tables)  

---

**Bước tiếp theo:**
1. Chạy SQL script trong DBeaver
2. API sẽ tự động kết nối với database
3. Test APIs via Postman collection
