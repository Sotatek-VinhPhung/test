"""
RBAC Usage Examples in ASP.NET Core Controllers
================================================

This file demonstrates practical examples of using the RBAC system
in various controller scenarios.
"""

## Example 1: Simple Authorization Check

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly IReportRepository _reportRepository;
    
    public ReportsController(
        IUserContextService userContextService,
        IReportRepository reportRepository)
    {
        _userContextService = userContextService;
        _reportRepository = reportRepository;
    }
    
    // GET: api/reports
    [HttpGet]
    public async Task<IActionResult> GetReports()
    {
        var userId = GetCurrentUserId();
        
        // Check if user has View permission on Reports subsystem
        if (!await _userContextService.HasPermissionAsync(
            userId, "Reports", Permission.View))
            return Forbid("You don't have permission to view reports");
        
        var reports = await _reportRepository.GetAllAsync();
        return Ok(reports);
    }
    
    // GET: api/reports/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var userId = GetCurrentUserId();
        
        // Check permission
        if (!await _userContextService.HasPermissionAsync(
            userId, "Reports", Permission.View))
            return Forbid();
        
        var report = await _reportRepository.GetByIdAsync(id);
        if (report == null)
            return NotFound();
        
        return Ok(report);
    }
    
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? 
                         User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException();
        
        return userId;
    }
}
```

## Example 2: Multi-Permission Requirements

```csharp
[HttpPost("{id}/approve")]
public async Task<IActionResult> ApproveReport(Guid id)
{
    var userId = GetCurrentUserId();
    
    // User MUST have ALL of these permissions
    var requiredPermissions = new[]
    {
        Permission.View,      // Can view reports
        Permission.Edit,      // Can edit reports
        Permission.Approve    // Can approve reports
    };
    
    if (!await _userContextService.HasAllPermissionsAsync(
        userId, "Reports", requiredPermissions))
        return Forbid("You need View, Edit, and Approve permissions");
    
    var report = await _reportRepository.GetByIdAsync(id);
    if (report == null)
        return NotFound();
    
    report.ApprovedAt = DateTime.UtcNow;
    report.ApprovedBy = userId;
    await _reportRepository.UpdateAsync(report);
    
    return Ok(report);
}
```

## Example 3: Conditional UI Response

```csharp
[HttpGet("{id}/details")]
public async Task<IActionResult> GetReportDetails(Guid id)
{
    var userId = GetCurrentUserId();
    
    // Load user's complete permission context
    var userContext = await _userContextService.GetUserContextAsync(userId);
    if (userContext == null)
        return Unauthorized();
    
    // Check individual permissions for UI display
    var canView = userContext.HasPermission("Reports", Permission.View);
    var canEdit = userContext.HasPermission("Reports", Permission.Edit);
    var canDelete = userContext.HasPermission("Reports", Permission.Delete);
    var canExport = userContext.HasPermission("Reports", Permission.Export);
    var canApprove = userContext.HasPermission("Reports", Permission.Approve);
    
    if (!canView)
        return Forbid();
    
    var report = await _reportRepository.GetByIdAsync(id);
    if (report == null)
        return NotFound();
    
    // Return detailed response with permission flags for UI
    return Ok(new
    {
        report = report,
        permissions = new
        {
            canView,
            canEdit,
            canDelete,
            canExport,
            canApprove
        }
    });
}
```

## Example 4: Resource Filtering by Permission

```csharp
[HttpGet("my-accessible")]
public async Task<IActionResult> GetMyAccessibleReports()
{
    var userId = GetCurrentUserId();
    
    // Get user's permission context
    var context = await _userContextService.GetUserContextAsync(userId);
    if (context == null)
        return Unauthorized();
    
    // If user doesn't have View permission, return empty list
    if (!context.HasPermission("Reports", Permission.View))
        return Ok(new List<ReportDto>());
    
    // Load all reports
    var allReports = await _reportRepository.GetAllAsync();
    
    // Apply additional filtering based on other permissions
    var accessibleReports = allReports
        .Select(r => new ReportDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            // Only include edit/delete endpoints in DTO if user has permission
            CanEdit = context.HasPermission("Reports", Permission.Edit),
            CanDelete = context.HasPermission("Reports", Permission.Delete),
            CanExport = context.HasPermission("Reports", Permission.Export),
            CanApprove = context.HasPermission("Reports", Permission.Approve)
        })
        .ToList();
    
    return Ok(accessibleReports);
}
```

## Example 5: Role-Based Operations

```csharp
[HttpPost("{id}/export")]
public async Task<IActionResult> ExportReport(Guid id)
{
    var userId = GetCurrentUserId();
    
    // Check specific permission
    if (!await _userContextService.HasPermissionAsync(
        userId, "Reports", Permission.Export))
        return Forbid("You don't have permission to export reports");
    
    var report = await _reportRepository.GetByIdAsync(id);
    if (report == null)
        return NotFound();
    
    // Generate export...
    var exportData = GenerateExportData(report);
    
    return File(exportData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{report.Name}_export.xlsx");
}

[HttpPost("{id}/schedule")]
public async Task<IActionResult> ScheduleReport(Guid id, ScheduleReportRequest request)
{
    var userId = GetCurrentUserId();
    
    // Check for specific subsystem permission (Reports.ScheduleReports)
    if (!await _userContextService.HasPermissionAsync(
        userId, "Reports", Permission.ScheduleReports))
        return Forbid("You don't have permission to schedule reports");
    
    var report = await _reportRepository.GetByIdAsync(id);
    if (report == null)
        return NotFound();
    
    // Schedule report...
    var schedule = new ReportSchedule
    {
        ReportId = id,
        Frequency = request.Frequency,
        NextRunAt = request.NextRunAt,
        CreatedBy = userId
    };
    
    await _reportRepository.AddScheduleAsync(schedule);
    
    return CreatedAtAction(nameof(GetReport), new { id = schedule.Id }, schedule);
}
```

## Example 6: Users Management Controller

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly RoleRepository _roleRepository;
    
    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var userId = GetCurrentUserId();
        
        // Only Manager+ can create users (requires ManageUsers permission)
        if (!await _userContextService.HasPermissionAsync(
            userId, "Users", Permission.ManageUsers))
            return Forbid("You don't have permission to create users");
        
        // Create user...
        var newUser = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BcryptPasswordHasher.Hash(request.Password)
        };
        
        await _userRepository.AddAsync(newUser);
        
        return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, newUser);
    }
    
    // PUT: api/users/{id}/role
    [HttpPut("{id}/role")]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest request)
    {
        var currentUserId = GetCurrentUserId();
        
        // Only admin can assign roles
        if (!await _userContextService.HasPermissionAsync(
            currentUserId, "Users", Permission.ManageRoles))
            return Forbid("You don't have permission to assign roles");
        
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();
        
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
            return NotFound("Role not found");
        
        // Remove existing roles
        await _userRepository.RemoveAllRolesAsync(id);
        
        // Assign new role
        var userRole = new UserRole
        {
            UserId = id,
            RoleId = request.RoleId,
            AssignedAt = DateTime.UtcNow
        };
        
        await _userRepository.AddRoleAsync(userRole);
        
        // Invalidate user's permission cache
        // (implement in your caching layer)
        
        return Ok(new { message = "Role assigned successfully" });
    }
}
```

## Example 7: Admin Settings Controller

```csharp
[ApiController]
[Route("api/admin/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly PermissionChecker _permissionChecker;
    
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var userId = GetCurrentUserId();
        
        // Only admins can access settings
        if (!await _userContextService.HasPermissionAsync(
            userId, "Settings", Permission.ManagePermissions))
            return Forbid();
        
        var settings = await _settingsRepository.GetAllAsync();
        return Ok(settings);
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateSettings(UpdateSettingsRequest request)
    {
        var userId = GetCurrentUserId();
        
        // Check for combined permissions
        var hasPermissions = await _permissionChecker.HasAllPermissionsAsync(
            userId, "Settings",
            Permission.View,
            Permission.Edit,
            Permission.ManagePermissions);
        
        if (!hasPermissions)
            return Forbid("Insufficient permissions for this operation");
        
        // Update settings...
        var settings = await _settingsRepository.GetAsync();
        // Apply request changes...
        await _settingsRepository.UpdateAsync(settings);
        
        return Ok(settings);
    }
}
```

## Example 8: Permission Check Helper Usage

```csharp
public class AnalyticsController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly PermissionChecker _permissionChecker;
    
    // Example using PermissionChecker helper
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();
        
        // Get user context once
        var context = await _permissionChecker.GetUserContextAsync(userId);
        if (context == null)
            return Unauthorized();
        
        // Multiple checks using static helper (no async overhead)
        var canViewReports = PermissionChecker.Static.HasPermission(
            context, "Reports", Permission.View);
        
        var canViewAnalytics = PermissionChecker.Static.HasPermission(
            context, "Analytics", Permission.View);
        
        var canExecuteQueries = PermissionChecker.Static.HasPermission(
            context, "Analytics", Permission.Execute);
        
        if (!canViewAnalytics)
            return Forbid();
        
        // Build dashboard response with conditional data
        var dashboard = new
        {
            reports = canViewReports ? await LoadReportData() : null,
            analytics = await LoadAnalyticsData(),
            canExecuteCustomQueries = canExecuteQueries
        };
        
        return Ok(dashboard);
    }
}
```

## Example 9: Custom Middleware for Automatic Permission Checks

```csharp
public class PermissionValidationMiddleware
{
    private readonly RequestDelegate _next;
    
    public PermissionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, IUserContextService userContextService)
    {
        var endpoint = context.GetEndpoint();
        var requiresPermission = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>();
        
        if (requiresPermission != null && context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("sub");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var hasPermission = await userContextService.HasPermissionAsync(
                    userId,
                    requiresPermission.SubsystemCode,
                    requiresPermission.Permission);
                
                if (!hasPermission)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Permission denied");
                    return;
                }
            }
        }
        
        await _next(context);
    }
}

// Custom attribute for marking endpoints
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute
{
    public string SubsystemCode { get; }
    public Permission Permission { get; }
    
    public RequirePermissionAttribute(string subsystemCode, Permission permission)
    {
        SubsystemCode = subsystemCode;
        Permission = permission;
    }
}

// Usage:
[HttpPost("create-report")]
[RequirePermission("Reports", Permission.Create)]
public async Task<IActionResult> CreateReport(CreateReportRequest request)
{
    // Middleware already validated permission
    // ... implementation ...
}
```

## Tips & Best Practices

1. **Always validate permissions** before performing sensitive operations
2. **Cache UserContext** to avoid repeated database queries
3. **Use HasAllPermissions** for restrictive operations requiring multiple permissions
4. **Return appropriate HTTP status codes:**
   - `401 Unauthorized` - No valid authentication token
   - `403 Forbidden` - User authenticated but lacks permission
5. **Log permission denials** for security auditing
6. **Include permission flags in API responses** for UI to show/hide buttons
7. **Preload user context** in middleware for request-wide access
8. **Validate permissions** on both client and server side

---

See `RBAC_QUICK_REFERENCE.md` for quick lookup and `RBAC_IMPLEMENTATION_GUIDE.md` for comprehensive documentation.
