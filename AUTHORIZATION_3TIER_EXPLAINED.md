# 📋 GIẢI THÍCH: Tên Bảng Database & Authorization Flow

## 1️⃣ Tên Bảng: PascalCase → snake_case

### Entity Framework Core Mapping

**C# DbSet (PascalCase):**
```csharp
public DbSet<User> Users => Set<User>();
public DbSet<Role> Roles => Set<Role>();
public DbSet<UserRole> UserRoles => Set<UserRole>();
public DbSet<Subsystem> Subsystems => Set<Subsystem>();
public DbSet<RoleSubsystemPermission> RoleSubsystemPermissions => Set<RoleSubsystemPermission>();
public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
public DbSet<Region> Regions => Set<Region>();
public DbSet<Company> Companies => Set<Company>();
public DbSet<Department> Departments => Set<Department>();
public DbSet<RoleOrganizationScope> RoleOrganizationScopes => Set<RoleOrganizationScope>();
```

**PostgreSQL Tên Bảng (snake_case) - Automatic by Npgsql Convention:**
```sql
-- Users
"users"
"user_roles"
"user_permission_overrides"

-- RBAC
"roles"
"subsystems"
"role_subsystem_permissions"

-- Organization Hierarchy
"regions"
"companies"
"departments"
"role_organization_scopes"
```

### Cách EF Core Mapping Hoạt động

```csharp
// 1. Implicit (tự động snake_case)
public DbSet<UserRole> UserRoles => Set<UserRole>();
// → Bảng: "user_roles"

// 2. Explicit (chỉ định tên cụ thể)
builder.ToTable("user_permission_overrides");
// → Bảng: "user_permission_overrides"

// 3. Configuration
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles"); // ← Tên bảng cụ thể
    }
}
```

## 2️⃣ Authorization Flow (3-Tier Model)

### 📊 Flow Diagram

```
API Request with JWT Token
    ↓
[PermissionAuthorizationHandler]
    ↓
Extract User ID & Required Permission
    ↓
┌─────────────────────────────────────────────────────────┐
│ TIER 1: RBAC (Role-Based Access Control)                │
│ ✅ Check: User has role?                                │
│ ✅ Check: Role has permission on subsystem?             │
└─────────────────────────────────────────────────────────┘
    ↓ (if TIER 1 pass)
┌─────────────────────────────────────────────────────────┐
│ TIER 2: ABAC (Attribute-Based Access Control)           │
│ ✅ Check: Role scoped to user's organization?           │
│ ✅ Check: User's region matches role scope?             │
│ ✅ Check: User's company matches role scope?            │
│ ✅ Check: User's department matches role scope?         │
└─────────────────────────────────────────────────────────┘
    ↓ (if TIER 2 pass)
┌─────────────────────────────────────────────────────────┐
│ TIER 3: Entity-Level (Optional)                         │
│ ✅ Check: Target scope in request?                      │
│ ✅ Verify: User can access target scope?                │
└─────────────────────────────────────────────────────────┘
    ↓
✅ Succeed (200) / ❌ Fail (403)
```

### 📝 Code Flow Chi tiết

**File: `PermissionAuthorizationHandler.cs`**

```csharp
protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context, PermissionRequirement requirement)
{
    // Step 1: Extract User ID from JWT
    var userId = ExtractUserIdFromJwt(context);
    
    // Step 2: Get Subsystem from database
    var subsystem = await dbContext.Subsystems
        .FirstOrDefaultAsync(s => s.Code == requirement.Module);
    
    // ✅ TIER 1+2: RBAC + ABAC in User's Organization
    var hasPermission = await hierarchicalPermissionService
        .HasPermissionInUserScopeAsync(userId, subsystem.Id, permission);
    
    if (hasPermission)
    {
        context.Succeed(requirement);
        return;
    }
    
    // ✅ TIER 3: Optional - Check Target Scope
    var targetRegionId = ExtractClaimAsGuid(context, "target_region_id");
    var targetCompanyId = ExtractClaimAsGuid(context, "target_company_id");
    var targetDepartmentId = ExtractClaimAsGuid(context, "target_department_id");
    
    var hasPermissionInTargetScope = await hierarchicalPermissionService
        .HasPermissionInScopeAsync(
            userId, subsystem.Id, permission,
            targetRegionId, targetCompanyId, targetDepartmentId);
    
    if (hasPermissionInTargetScope)
    {
        context.Succeed(requirement);
    }
}
```

### 🔍 Ví dụ: User "Manager - Hanoi" Access Report

**User Info:**
- ID: `50000000-0000-0000-0000-000000000002`
- Name: "Nguyễn Văn A"
- Role: **Manager** (code: "MANAGER")
- Organization:
  - Region: **VN-N** (Hanoi)
  - Company: **ABC-Corp**
  - Department: **Accounting**

**Request:**
```http
POST /api/reports/generate
Authorization: Bearer <JWT with sub=50000000-0000-0000-0000-000000000002>
```

**Check Flow:**

1. **TIER 1: RBAC Check**
   ```sql
   SELECT rsp.flags
   FROM role_subsystem_permissions rsp
   JOIN user_roles ur ON ur.role_id = rsp.role_id
   WHERE ur.user_id = '50000000-0000-0000-0000-000000000002'
     AND rsp.subsystem_id = (SELECT id FROM subsystems WHERE code = 'REPORT_SUBSYSTEM');
   -- Result: flags = 28 (Read|Create|Update) ✅ PASS
   ```

2. **TIER 2: ABAC Check (Organization Scope)**
   ```sql
   SELECT *
   FROM role_organization_scopes ros
   JOIN regions r ON ros.region_id = r.id
   WHERE ros.role_id = (SELECT id FROM roles WHERE code = 'MANAGER')
     AND r.code = 'VN-N'
     AND ros.company_id IS NULL  -- Scope applies to whole region
     AND ros.department_id IS NULL;
   -- Result: Found ✅ PASS (Role is scoped to VN-N region)
   ```

3. **Result: ✅ SUCCEED (Authorized)**

---

## 3️⃣ Database Diagram

```
┌─────────────────────────────────────────────────────────┐
│                       USERS                             │
├─────────────────────────────────────────────────────────┤
│ id (PK)                                                 │
│ first_name, last_name, email, password_hash            │
│ region_id (FK) ────────────┐                           │
│ company_id (FK) ──────┐    │                           │
│ department_id (FK) ───┼─┐  │                           │
└─────────────────────────────────────────────────────────┘
          │ 1-to-N      │ │  │
          │             │ │  │
    ┌─────▼──────┐      │ │  │
    │ USER_ROLES │      │ │  │
    │ (user_id FK│      │ │  │
    │  role_id FK│      │ │  │
    └──────┬──────┘      │ │  │
           │             │ │  │
      ┌────▼──────────────┘ │  │
      │  ROLES              │  │
      │ (RBAC Layer)        │  │
      │ id, code, name      │  │
      └────┬────────────────┘  │
           │                   │
      ┌────┴─────────────────────────────────┐
      │ ROLE_ORGANIZATION_SCOPES (ABAC)      │
      │ ├─ region_id (FK) ──────────┐       │
      │ ├─ company_id (FK) ─┐       │       │
      │ └─ department_id (FK) ───┐ │       │
      └──────────────────────────┼─┼─┐     │
                                  │ │ │     │
                    ┌─────────────┘ │ │     │
                    │               │ │     │
              ┌─────▼───────┐       │ │     │
              │   REGIONS   │       │ │     │
              │ (VN-N, etc) │       │ │     │
              └─────────────┘       │ │     │
                                    │ │     │
                    ┌───────────────┘ │     │
                    │                 │     │
              ┌─────▼──────────┐      │     │
              │   COMPANIES    │      │     │
              │ (ABC-Corp)     │      │     │
              └─────────────────┘      │     │
                                       │     │
                      ┌────────────────┘     │
                      │                      │
                ┌─────▼────────────┐        │
                │  DEPARTMENTS     │        │
                │ (Accounting,HR)  │        │
                └──────────────────┘        │
                                            │
           ┌────────────────────────────────┘
           │
      ┌────▼───────────────────────────┐
      │ ROLE_SUBSYSTEM_PERMISSIONS      │
      │ (RBAC - phần quyền)             │
      │ role_id FK                      │
      │ subsystem_id FK ───┐            │
      │ flags (bitwise)    │            │
      └────────────────────┼────────────┘
                           │
                    ┌──────▼──────────┐
                    │  SUBSYSTEMS     │
                    │ (Reports, Users)│
                    └─────────────────┘
```

## 4️⃣ Bảng Mapping Tóm tắt

| C# Entity | PostgreSQL Bảng | Mục Đích |
|-----------|-----------------|---------|
| `User` | `"users"` | Người dùng hệ thống |
| `Role` | `"roles"` | Vai trò (Admin, Manager...) |
| `UserRole` | `"user_roles"` | Gán vai trò cho người dùng |
| `Subsystem` | `"subsystems"` | Hệ thống con (Reports, Users) |
| `RoleSubsystemPermission` | `"role_subsystem_permissions"` | Quyền trên hệ thống con |
| `UserPermissionOverride` | `"user_permission_overrides"` | Override quyền cá nhân |
| `Region` | `"regions"` | Khu vực (VN-N, SG) |
| `Company` | `"companies"` | Công ty |
| `Department` | `"departments"` | Phòng ban |
| `RoleOrganizationScope` | `"role_organization_scopes"` | Giới hạn vai trò theo tổ chức |

---

**✅ Update Complete!** `PermissionAuthorizationHandler` giờ đã support 3-tier authorization:
- ✅ TIER 1: RBAC
- ✅ TIER 2: ABAC (region/company/department)
- ✅ TIER 3: Entity-level (target scope)
