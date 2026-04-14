# RBAC Quick Reference

## 📦 Permission Enum Values

```csharp
View                = 1       (binary: 0000001)
Create              = 2       (binary: 0000010)
Edit                = 4       (binary: 0000100)
Delete              = 8       (binary: 0001000)
Export              = 16      (binary: 0010000)
Approve             = 32      (binary: 0100000)
Execute             = 64      (binary: 1000000)
Audit               = 128     (binary: 10000000)
ManageUsers         = 256     (binary: 100000000)
ViewReports         = 512     (binary: 1000000000)
EditReports         = 1024    (binary: 10000000000)
ScheduleReports     = 2048    (binary: 100000000000)
ManageRoles         = 4096    (binary: 1000000000000)
ManagePermissions   = 8192    (binary: 10000000000000)
```

## 🏢 Default Subsystems

| Code | Name | Purpose |
|------|------|---------|
| `Reports` | Reports Module | Access to reports and dashboards |
| `Users` | Users Management | User and account management |
| `Analytics` | Analytics Module | Advanced analytics and insights |
| `Settings` | Settings Module | System configuration |
| `Audit` | Audit Logs | Audit trail and logging |

## 👥 Default Roles

| Code | Name | Permissions |
|------|------|-------------|
| `Admin` | Administrator | Full access on all subsystems |
| `Manager` | Manager | View, Create, Edit, Approve |
| `Editor` | Editor | View, Create, Edit |
| `Viewer` | Viewer | View only |

## 🔍 Permission Checking

### Single Permission Check
```csharp
// Using service
bool canView = await _userContextService.HasPermissionAsync(
    userId, "Reports", Permission.View
);

// Using context directly
if (context.HasPermission("Reports", Permission.View))
{
    // User can view
}
```

### Multiple Permissions (ALL required)
```csharp
// All permissions must be present
bool hasAll = await _userContextService.HasAllPermissionsAsync(
    userId, "Reports",
    new[] { Permission.View, Permission.Edit, Permission.Approve }
);

// Or using context
if (context.HasAllPermissions("Reports", 
    Permission.View, Permission.Edit, Permission.Approve))
{
    // User has all permissions
}
```

### Get User Context
```csharp
var context = await _userContextService.GetUserContextAsync(userId);

// Properties available:
// - context.UserId
// - context.Email
// - context.RoleIds
// - context.SubsystemPermissions (Dictionary<string, long>)
// - context.RegionIds (optional)
// - context.DepartmentIds (optional)
```

## 🔌 API Endpoints

### Get Current User Permissions
```
GET /api/permissions/me
Response: { userId, email, roleIds, subsystemPermissions }
```

### Get Specific Subsystem Permissions
```
GET /api/permissions/subsystems/Reports
Response: { subsystem, permissions: {view: true, create: false, ...}, rawFlags }
```

### Check Single Permission
```
GET /api/permissions/subsystems/Reports/check/View
Response: { subsystem, permission, granted }
```

### Get Available Subsystems
```
GET /api/permissions/subsystems
Response: [{ id, code, name, description }, ...]
```

### Get Available Permissions
```
GET /api/permissions/available
Response: [{ name, value, description }, ...]
```

### Get All Roles (Admin only)
```
GET /api/permissions/roles
Response: [{ id, code, name, description }, ...]
```

## 💾 Database Queries

### Assign Role to User
```sql
INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
VALUES ('user-id', 'role-id', GETUTCDATE());
```

### Grant Permission to Role
```sql
INSERT INTO RoleSubsystemPermissions (RoleId, SubsystemId, Flags)
VALUES ('role-id', 'subsystem-id', 7) -- View(1) | Create(2) | Edit(4)
ON CONFLICT (RoleId, SubsystemId) DO UPDATE
SET Flags = 7, UpdatedAt = GETUTCDATE();
```

### Get User's Roles
```sql
SELECT r.* FROM Roles r
INNER JOIN UserRoles ur ON r.Id = ur.RoleId
WHERE ur.UserId = 'user-id' AND ur.ExpiresAt IS NULL;
```

### Get Role Permissions
```sql
SELECT s.Code, rsp.Flags
FROM RoleSubsystemPermissions rsp
INNER JOIN Subsystems s ON rsp.SubsystemId = s.Id
WHERE rsp.RoleId = 'role-id';
```

## 🛠️ Common Scenarios

### Scenario 1: Grant View + Create to Role on Reports
```csharp
var flags = Permission.View | Permission.Create; // = 3
var permission = new RoleSubsystemPermission
{
    RoleId = roleId,
    SubsystemId = reportsSubsystemId,
    Flags = (long)flags
};

await _roleRepository.GrantPermissionAsync(roleId, reportsSubsystemId, (long)flags);
```

### Scenario 2: Check if User Can Perform Action
```csharp
var requiredPermissions = new[] 
{ 
    Permission.View,   // Must be able to view
    Permission.Edit,   // Must be able to edit
    Permission.Approve // Must be able to approve
};

bool canPerform = context.HasAllPermissions("Reports", requiredPermissions);
```

### Scenario 3: Conditional UI Display
```csharp
// In API - return permission flags
var permissions = await _userContextService.GetSubsystemPermissionsAsync(
    userId, "Reports"
);

// Transform to UI-friendly format
var uiPermissions = new
{
    canView = permissions.HasPermission(Permission.View),
    canCreate = permissions.HasPermission(Permission.Create),
    canEdit = permissions.HasPermission(Permission.Edit),
    canDelete = permissions.HasPermission(Permission.Delete),
    canExport = permissions.HasPermission(Permission.Export),
    canApprove = permissions.HasPermission(Permission.Approve)
};

return Ok(uiPermissions);
```

### Scenario 4: Reload After Permission Change
```csharp
// Admin changes user's role
await roleRepository.RemoveUserRoleAsync(userId, oldRoleId);
await roleRepository.AddUserRoleAsync(userId, newRoleId);

// Reload permission context to reflect changes
var updatedContext = await _userContextService.ReloadUserContextAsync(userId);

// Or clear cache if using Redis
await cache.RemoveAsync($"user:perms:{userId}");
```

### Scenario 5: Multi-Role Permission Merging
```
User1 has roles: [Manager, Editor]

Manager on Reports: View(1) | Create(2) | Approve(32) = 35
Editor on Reports:  View(1) | Create(2) | Edit(4) = 7

Merged (OR):        View(1) | Create(2) | Edit(4) | Approve(32) = 39

User1 can: View, Create, Edit, Approve on Reports
```

## 🔄 Extension Points

### Add Custom Permissions
```csharp
public enum Permission : long
{
    // ... existing permissions ...
    Custom1 = 1 << 14,  // 16384
    Custom2 = 1 << 15,  // 32768
}
```

### Add Custom Subsystems
```sql
INSERT INTO Subsystems (Id, Code, Name, Description)
VALUES (NEWID(), 'CustomModule', 'Custom Module', 'Description');
```

### Extend UserContext
```csharp
// Add custom properties for multi-tenant scenarios
public class UserContext
{
    public IReadOnlyList<Guid>? TenantIds { get; }
    public IReadOnlyList<Guid>? ProjectIds { get; }
}
```

## ⚡ Performance Tips

1. **Cache UserContext** - Reduces DB queries from every request to once per TTL
2. **Use static helpers** - `PermissionChecker.Static.*` for multiple checks
3. **Batch permission checks** - Load context once, check multiple times
4. **Index frequently queried columns** - UserRoles.UserId, RoleSubsystemPermissions.SubsystemId
5. **Avoid redundant checks** - Cache permission decision in request context

## 🆘 Troubleshooting

| Issue | Solution |
|-------|----------|
| User has no permissions | Check UserRoles table for role assignment |
| Role has no permissions | Check RoleSubsystemPermissions for role-subsystem mapping |
| Permission string not recognized | Use exact enum name: `Permission.ViewReports` not `Permission.view_reports` |
| Forbid() returns 403 when expected | Verify [Authorize] attribute and JWT token is valid |
| Permissions cached incorrectly | Clear cache: `await cache.RemoveAsync($"user:perms:{userId}")` |

---

**Helpful Links:**
- Full Guide: `RBAC_IMPLEMENTATION_GUIDE.md`
- Setup Script: `database/setup_rbac_system.sql`
- API Controller: `src/CleanArchitecture.Api/Controllers/PermissionsController.cs`
