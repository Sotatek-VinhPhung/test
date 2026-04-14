# ✅ RBAC Migration Complete

## 📋 Summary

Bạn đã **chuyển hoàn toàn sang RBAC mới** (Subsystem-based). Dưới đây là tất cả những gì đã thay đổi:

---

## 🗑️ **Những Gì Đã Xóa**

### 1. Legacy Seeder
- ❌ `src\CleanArchitecture.Infrastructure\Persistence\Seed\PermissionSeeder.cs` - **DELETED**

### 2. AppDbContext - DbSets Cũ
```csharp
// ❌ REMOVED
public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
```

### 3. Database Tables (sẽ xóa khi migrate)
```sql
-- ❌ REMOVED via migration
DROP TABLE UserPermissionOverrides;
DROP TABLE RolePermissions;
```

### 4. Legacy Code Disabled
- `PermissionRepository` - Mark obsolete, throw NotImplementedException
- `PermissionSeeder` - Completely removed
- Legacy methods in `RoleManagementService` - Marked [Obsolete]

---

## ✨ **Những Gì Đã Thêm**

### 1. New RBAC Seeder
✅ `src\CleanArchitecture.Infrastructure\Persistence\Seed\RbacSeeder.cs`

Seed dữ liệu:
- **6 Subsystems**: Reports, Users, Analytics, Settings, Audit, Orders
- **4 Roles**: Admin, Manager, Editor, Viewer
- **15 RoleSubsystemPermissions** với phân quyền chi tiết

### 2. Updated Files
✅ `SeedExtensions.cs`
```csharp
// OLD
await app.SeedPermissionsAsync();

// NEW
await app.SeedRbacAsync();
```

✅ `Program.cs`
```csharp
// OLD
await app.SeedPermissionsAsync();

// NEW
await app.SeedRbacAsync();
```

✅ `AppDbContext.cs` - Removed legacy DbSets

### 3. Database Migration
✅ `src\CleanArchitecture.Infrastructure\Migrations\20260414091625_RemoveLegacyRbacTables.cs`

Xóa:
- `UserPermissionOverrides` table
- `RolePermissions` table

---

## 🚀 **Cách Sử Dụng RBAC Mới**

### 1. Kiểm tra Quyền (Permission Check)
```csharp
// Kiểm tra user có quyền View Reports không
var hasPermission = await _permissionService.HasPermissionAsync(
    userId,
    Permission.View,
    Subsystem.WellKnown.Reports);
```

### 2. Gán Role Cho User
```csharp
// Assign Admin role to user
await _roleManagementService.AssignRoleToUserAsync(userId, roleId);

// Revoke Admin role from user
await _roleManagementService.RevokeRoleFromUserAsync(userId, roleId);
```

### 3. Update Role Permissions
```csharp
// Grant Delete permission to Manager role on Reports subsystem
await _roleManagementService.UpdateRolePermissionsAsync(
    managerId,
    reportsId,
    (long)(Permission.View | Permission.Create | Permission.Edit | Permission.Delete)
);
```

### 4. Use in API Endpoint
```csharp
[HttpGet("reports")]
[Authorize]
[RequirePermission(Permission.View, Subsystem.WellKnown.Reports)]
public async Task<IActionResult> GetReports()
{
    // Only accessible if user has View permission on Reports
    return Ok(await _reportService.GetAllAsync());
}
```

---

## 📊 Default Permissions After Seeding

### Admin (Full Access)
```
Reports:   View | Create | Edit | Delete | Export | Approve | Execute | Audit | ManageUsers | ManageRoles | ManagePermissions
Users:     View | Create | Edit | Delete | Export | Approve | Execute | Audit | ManageUsers | ManageRoles | ManagePermissions
Analytics: View | Create | Edit | Delete | Export | Approve | Execute | Audit | ManageUsers | ManageRoles | ManagePermissions
Settings:  View | Create | Edit | Delete | Export | Approve | Execute | Audit | ManageUsers | ManageRoles | ManagePermissions
Audit:     View | Create | Edit | Delete | Export | Approve | Execute | Audit | ManageUsers | ManageRoles | ManagePermissions
Orders:    View | Create | Edit | Delete | Export | Approve | Execute | Audit | ManageUsers | ManageRoles | ManagePermissions
```

### Manager (Restricted Access)
```
Reports:   View | Create | Edit | Approve | ManageUsers
Users:     View | Create | Edit | Approve | ManageUsers
Orders:    View | Create | Edit | Approve | ManageUsers
Analytics: View only
```

### Editor (Limited Access)
```
Reports:   View | Create | Edit
Orders:    View | Create | Edit
Users:     View only
```

### Viewer (Read-Only)
```
Reports:   View only
Analytics: View only
Orders:    View only
```

---

## 🔄 Next Steps

### 1. Apply Database Migration
```bash
# When ready to migrate production database
dotnet ef database update -p src/CleanArchitecture.Infrastructure/ -s src/CleanArchitecture.Api/
```

### 2. Test Seeding
```bash
# Run the app to trigger seeding
dotnet run --project src/CleanArchitecture.Api/

# Check database:
# SELECT * FROM "Subsystems";
# SELECT * FROM "Roles";
# SELECT * FROM "RoleSubsystemPermissions";
```

### 3. Assign Users to Roles
```csharp
// Create a test user and assign role
var userId = Guid.NewGuid();
var adminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
await _roleManagementService.AssignRoleToUserAsync(userId, adminRoleId);
```

### 4. Test Permission Check
```bash
# Call an endpoint with [RequirePermission(Permission.View, Subsystem.WellKnown.Reports)]
curl -X GET "https://localhost:5000/api/reports" \
  -H "Authorization: Bearer {JWT_TOKEN}"

# If user has permission: 200 OK
# If not: 403 Forbidden
```

---

## 🛠️ Troubleshooting

### Q: Build fails with "Type or member is obsolete"
**A:** That's expected. Legacy code is marked as obsolete but still in DI for backward compatibility.
- Use `#pragma warning disable CS0618` if needed
- New code should not reference legacy classes

### Q: RolePermissions/UserPermissionOverrides table error
**A:** Run the migration:
```bash
dotnet ef database update
```

### Q: Seeding not working
**A:** Check `RbacSeeder.cs` logs:
```csharp
System.Diagnostics.Debug.WriteLine($"RBAC seeding error: {ex.Message}");
```

### Q: Permission check always fails
**A:** Verify:
1. User has a role assigned (UserRole record exists)
2. Role has RoleSubsystemPermission for that subsystem
3. Subsystem.Code matches what you're checking

---

## 📚 Key Files

| File | Purpose |
|------|---------|
| `RbacSeeder.cs` | Seed RBAC data (Subsystems, Roles, Permissions) |
| `RoleRepository.cs` | Query roles & user roles |
| `SubsystemRepository.cs` | Query subsystems |
| `PermissionService.cs` | Check user permissions |
| `RoleManagementService.cs` | Assign/revoke roles, update permissions |
| `PermissionAuthorizationHandler.cs` | Enforce [RequirePermission] attribute |
| `RbacSeeder.cs` | Seed default roles & permissions |

---

## ✅ Checklist

- [x] Deleted legacy `PermissionSeeder.cs`
- [x] Removed legacy DbSets from AppDbContext
- [x] Created new `RbacSeeder.cs`
- [x] Updated `SeedExtensions.cs` to use RbacSeeder
- [x] Updated `Program.cs` to call new seeder
- [x] Marked legacy code as [Obsolete]
- [x] Created migration to drop legacy tables
- [x] Build successful ✅
- [ ] Run `dotnet ef database update` (when ready)
- [ ] Test seeding in development
- [ ] Test permission checks on endpoints
- [ ] Deploy to production

---

## 🎯 Architecture Overview

```
User
  ↓
[Authorize] + [RequirePermission(perm, subsystem)]
  ↓
PermissionAuthorizationHandler
  ↓
PermissionService.HasPermissionAsync()
  ↓
RoleRepository.GetUserRolesAsync()
  ↓
SubsystemRepository.GetByCodeAsync()
  ↓
PermissionService queries RoleSubsystemPermission
  ↓
Check: (Flags & Permission) == Permission
  ↓
✓ Allowed / ✗ Forbidden
```

---

## 📞 Questions?

Refer to:
- `RBAC_DETAILED_ARCHITECTURE.md` - Full system design
- `RBAC_USAGE_EXAMPLES.md` - Code examples
- `RBAC_QUICK_REFERENCE.md` - Quick API reference
