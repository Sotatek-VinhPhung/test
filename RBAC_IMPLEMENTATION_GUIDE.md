# RBAC Implementation Guide - Clean Architecture Pattern

## 🏗️ Architecture Overview

This document describes the complete RBAC (Role-Based Access Control) system implementation following Clean Architecture principles.

### Layer Structure

```
┌─────────────────────────────────────────────────┐
│ API Layer (Controllers, Middleware)              │
│ ├─ PermissionsController (endpoints)             │
│ └─ Authorization policies                        │
└──────────────┬──────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────┐
│ Application Layer (Services, Use Cases)          │
│ ├─ IUserContextService (interface)               │
│ ├─ PermissionChecker (helpers)                   │
│ └─ DTOs & exceptions                             │
└──────────────┬──────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────┐
│ Domain Layer (Entities, Value Objects)           │
│ ├─ Role entity                                   │
│ ├─ Subsystem entity                              │
│ ├─ RoleSubsystemPermission entity                │
│ ├─ Permission enum (bitwise)                     │
│ ├─ UserContext value object                      │
│ └─ Interfaces (IUserContextService)              │
└──────────────┬──────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────┐
│ Infrastructure Layer (Data Access, Services)     │
│ ├─ UserContextServiceImpl (DB access)            │
│ ├─ Repositories (Role, Subsystem)                │
│ ├─ EF Core DbContext & Configurations           │
│ └─ Redis caching (future)                        │
└─────────────────────────────────────────────────┘
```

## 🔐 Core Concepts

### Permission Enum (Bitwise Flags)

Permissions are defined as bitwise flags, enabling efficient storage and checking:

```csharp
[Flags]
public enum Permission : long
{
    None = 0,
    View = 1 << 0,           // 1
    Create = 1 << 1,         // 2
    Edit = 1 << 2,           // 4
    Delete = 1 << 3,         // 8
    Export = 1 << 4,         // 16
    Approve = 1 << 5,        // 32
    // ... up to 64 permissions per subsystem
}
```

**Advantages:**
- O(1) permission checking with bitwise AND
- Single BIGINT (64-bit) stores all permissions per subsystem
- Minimal database storage
- Efficient serialization for JWT claims

### Permission Merging (Bitwise OR)

Multiple roles' permissions are merged using bitwise OR:

```
User has roles: [Admin, Manager]

Admin permissions on Reports:    01111111 (View|Create|Edit|Delete|Export|Approve)
Manager permissions on Reports:  00010011 (View|Create|Edit|Approve)

Merged result (OR):               01111111 (User gets all Admin permissions)
```

### UserContext Value Object

Immutable snapshot of user permissions, designed for caching:

```csharp
public class UserContext
{
    public Guid UserId { get; }
    public IReadOnlyList<Guid> RoleIds { get; }
    public IReadOnlyDictionary<string, long> SubsystemPermissions { get; }
    public IReadOnlyList<Guid>? RegionIds { get; }  // Optional
    public IReadOnlyList<Guid>? DepartmentIds { get; } // Optional
}
```

## 🔄 Data Model

### Tables

```sql
-- Role entities
Roles (Id, Code, Name, Description, IsActive, CreatedAt, UpdatedAt)

-- User-to-Role mapping (many-to-many)
UserRoles (UserId, RoleId, AssignedAt, ExpiresAt)

-- Functional subsystems
Subsystems (Id, Code, Name, Description, IsActive, CreatedAt)

-- Role permissions per subsystem
RoleSubsystemPermissions (RoleId, SubsystemId, Flags, UpdatedAt)
```

### Primary Keys & Relationships

- **Roles**: Id (UUID)
- **UserRoles**: Composite (UserId, RoleId)
- **Subsystems**: Id (UUID)
- **RoleSubsystemPermissions**: Composite (RoleId, SubsystemId)

## 📋 Usage Examples

### Example 1: Check Single Permission

```csharp
public class ReportsController : ControllerBase
{
    private readonly IUserContextService _userContextService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var userId = GetCurrentUserId();
        
        // Check if user can view reports
        var hasPermission = await _userContextService.HasPermissionAsync(
            userId, 
            "Reports",
            Permission.View
        );
        
        if (!hasPermission)
            return Forbid();
        
        // Load and return report
        return Ok(...);
    }
}
```

### Example 2: Check Multiple Permissions (Requires ALL)

```csharp
public async Task<IActionResult> ApproveReport(Guid id)
{
    var userId = GetCurrentUserId();
    
    // User must have both View AND Edit AND Approve
    var hasPermissions = await _userContextService.HasAllPermissionsAsync(
        userId,
        "Reports",
        new[] { Permission.View, Permission.Edit, Permission.Approve }
    );
    
    if (!hasPermissions)
        return Forbid();
    
    // Approve report...
    return Ok(...);
}
```

### Example 3: Get User's Complete Permissions

```csharp
public async Task<IActionResult> GetMyPermissions()
{
    var userId = GetCurrentUserId();
    var context = await _userContextService.GetUserContextAsync(userId);
    
    if (context == null)
        return NotFound();
    
    return Ok(new
    {
        userId = context.UserId,
        roles = context.RoleIds,
        subsystemPermissions = context.SubsystemPermissions,
        accessibleSubsystems = context.GetAccessibleSubsystems()
    });
}
```

### Example 4: Filter Resources by Permission

```csharp
public async Task<IActionResult> GetReports()
{
    var userId = GetCurrentUserId();
    
    // Load all reports
    var allReports = await _reportRepository.GetAllAsync();
    
    // Check if user can view reports
    var hasPermission = await _userContextService.HasPermissionAsync(
        userId,
        "Reports",
        Permission.View
    );
    
    if (!hasPermission)
        return Ok(new List<ReportDto>()); // Return empty list
    
    return Ok(allReports.Select(r => r.ToDto()));
}
```

### Example 5: Using Static Permission Checker

```csharp
// After loading UserContext once
var context = await _userContextService.GetUserContextAsync(userId);

// Multiple checks without async overhead
if (PermissionChecker.Static.HasPermission(context, "Reports", Permission.View))
{
    // Show report button
}

if (PermissionChecker.Static.HasAllPermissions(
    context, 
    "Reports", 
    Permission.Create, 
    Permission.Edit))
{
    // Show edit form
}
```

## 🛠️ Key Implementation Files

### Domain Layer

| File | Purpose |
|------|---------|
| `Entities/Role.cs` | Role entity with composition support |
| `Entities/UserRole.cs` | Junction table for User-Role relationship |
| `Entities/Subsystem.cs` | Functional subsystem entity |
| `Entities/RoleSubsystemPermission.cs` | Role-Subsystem permission mapping |
| `Enums/Permission.cs` | Bitwise permission flags + extensions |
| `ValueObjects/UserContext.cs` | Immutable permission snapshot |

### Application Layer

| File | Purpose |
|------|---------|
| `Permissions/Interfaces/IUserContextService.cs` | Permission loading contract |
| `Permissions/Services/UserContextServiceBase.cs` | Base abstract service |
| `Permissions/Helpers/PermissionChecker.cs` | Utility for permission checks |

### Infrastructure Layer

| File | Purpose |
|------|---------|
| `Persistence/Services/UserContextServiceImpl.cs` | DB implementation of IUserContextService |
| `Persistence/Repositories/RoleRepository.cs` | Role CRUD operations |
| `Persistence/Repositories/SubsystemRepository.cs` | Subsystem CRUD operations |
| `Persistence/Configurations/RoleConfiguration.cs` | EF Core role mapping |
| `Persistence/Configurations/UserRoleConfiguration.cs` | EF Core user-role mapping |
| `Persistence/Configurations/SubsystemConfiguration.cs` | EF Core subsystem mapping |
| `Persistence/Configurations/RoleSubsystemPermissionConfiguration.cs` | EF Core permission mapping |

### API Layer

| File | Purpose |
|------|---------|
| `Controllers/PermissionsController.cs` | Permission query endpoints |

## 🚀 Getting Started

### 1. Apply EF Core Migrations

```bash
dotnet ef database update
```

### 2. Seed Initial Data

Run the SQL setup script:

```bash
# From package manager console
sqlcmd -S .\SQLEXPRESS -i database\setup_rbac_system.sql

# Or execute manually in SQL Server Management Studio
```

### 3. Assign Roles to Users

```csharp
var userId = new Guid("...");
var managerRoleId = new Guid("10000000-0000-0000-0000-000000000002");

var userRole = new UserRole
{
    UserId = userId,
    RoleId = managerRoleId,
    AssignedAt = DateTime.UtcNow
};

context.UserRoles.Add(userRole);
await context.SaveChangesAsync();
```

### 4. Check Permissions in Controllers

```csharp
[Authorize]
[HttpGet("reports")]
public async Task<IActionResult> GetReports()
{
    var userId = GetCurrentUserId();
    
    if (!await _userContextService.HasPermissionAsync(
        userId, "Reports", Permission.View))
        return Forbid();
    
    return Ok(...);
}
```

### 5. Query User Permissions from API

```bash
# Get all permissions for current user
GET /api/permissions/me

# Get permissions for specific subsystem
GET /api/permissions/subsystems/Reports

# Check if user has specific permission
GET /api/permissions/subsystems/Reports/check/View

# Get available permissions (for UI)
GET /api/permissions/available

# Get available subsystems
GET /api/permissions/subsystems
```

## 📊 Performance Considerations

### Bitwise Operations
- **O(1)** permission checking: `(flags & required) == required`
- **No SQL queries** needed for checks after UserContext is loaded
- **Minimal network overhead**: Long serializes to 8 bytes in binary format

### Caching Strategy (Recommended)

```csharp
// Cache UserContext in Redis with 1-hour TTL
var cacheKey = $"user:perms:{userId}";
var cachedContext = await _cache.GetAsync(cacheKey);

if (cachedContext == null)
{
    cachedContext = await _userContextService.GetUserContextAsync(userId);
    await _cache.SetAsync(cacheKey, cachedContext, TimeSpan.FromHours(1));
}

// Invalidate on role/permission changes
await _cache.RemoveAsync(cacheKey);
```

### Database Queries
- Single query to load user with roles and permissions
- Uses eager loading: `Include().ThenInclude()`
- Result: **1 round-trip to database** per UserContext load

## 🔐 Security Considerations

### Permission Isolation
- Permissions are **per-subsystem** by default
- Multi-region/multi-department support via UserContext extension
- Role assignment can have **optional expiration** (ExpiresAt)

### Audit Trail
- All permission changes saved with **UpdatedAt timestamp**
- RoleSubsystemPermission has dedicated `UpdatedAt` field
- Consider logging permission checks for sensitive operations

### JWT Token Integration
- Permission flags serialized as **JSON in JWT claims**
- Reduces token size vs. including all permissions
- Server re-validates permissions on each request (no client-side trust)

## 🎯 Best Practices

1. **Always call `ReloadUserContextAsync`** after changing user roles/permissions
2. **Use static helpers for multiple checks** within single request context
3. **Cache UserContext** in production (Redis recommended)
4. **Implement cache invalidation** on role/permission changes
5. **Log permission denials** for audit and debugging
6. **Use `HasAllPermissions`** for restrictive policies, `HasAnyPermission` for flexible
7. **Define Permission values** as constants for easier maintenance
8. **Document subsystem codes** (Reports, Users, Analytics) in codebase

## 📈 Extension Points

### Multi-Region Support
```csharp
// UserContext can include RegionIds
if (context.IsInRegion(regionId))
{
    // Apply region-specific filters
}
```

### Multi-Department Support
```csharp
// UserContext can include DepartmentIds
if (context.IsInDepartment(departmentId))
{
    // Apply department-specific filters
}
```

### Dynamic Permission Definition
```csharp
// Future: Load permission definitions from PermissionDefinitions table
// Support for up to 64 dynamic permissions per subsystem
```

### Cache Integration (Redis)
```csharp
// Planned: Redis caching layer for UserContextService
// Automatic cache invalidation on role/permission changes
// Support for distributed systems
```

## 📞 Support

For questions or issues:
1. Check Permission enum for available flags
2. Verify user has roles via UserRoles table
3. Confirm roles have permissions via RoleSubsystemPermissions
4. Review PermissionsController endpoint responses
5. Check application logs for permission check details

---

**Last Updated:** 2024  
**Version:** 1.0.0
