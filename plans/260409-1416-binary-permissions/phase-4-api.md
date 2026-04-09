# Phase 4: API Authorization Pipeline

**Depends on**: Phase 3 | **Blocks**: Phase 5

## Goal

Implement [RequirePermission] attribute for controller-level enforcement using ASP.NET Core authorization framework. Permission flags read from JWT claims. No DB call on every request.

## How It Works

1. RequirePermissionAttribute sets Authorize(Policy = "Permission:Users:2")
2. PermissionPolicyProvider dynamically creates policy with PermissionRequirement
3. PermissionAuthorizationHandler reads perm:Users claim from JWT, does bitwise check

No DB call. Pure claim parsing.

---

## Files to Create

### 1. src/CleanArchitecture.Api/Authorization/RequirePermissionAttribute.cs

Extends AuthorizeAttribute with AllowMultiple = true.

Constructor: (PermissionModule module, long requiredFlags) sets Policy = "Permission:{module}:{requiredFlags}"

Usage on controllers:

    [RequirePermission(PermissionModule.Users, (long)UserPermissions.Read)]

The (long) cast is required because attribute parameters must be compile-time constants.

### 2. src/CleanArchitecture.Api/Authorization/PermissionRequirement.cs

Implements IAuthorizationRequirement. Two properties: Module (string), RequiredFlags (long).

### 3. src/CleanArchitecture.Api/Authorization/PermissionPolicyProvider.cs

Implements IAuthorizationPolicyProvider. Key logic:

- Falls back to DefaultAuthorizationPolicyProvider for non-permission policies
- Parses "Permission:Users:15" into module="Users", flags=15
- Builds AuthorizationPolicy with PermissionRequirement
- Implements GetDefaultPolicyAsync and GetFallbackPolicyAsync via fallback

### 4. src/CleanArchitecture.Api/Authorization/PermissionAuthorizationHandler.cs

Extends AuthorizationHandler<PermissionRequirement>. Key logic:

- Reads claim via PermissionClaimNames.ForModule(requirement.Module)
- Parses claim value to long
- Bitwise check: (userFlags & requirement.RequiredFlags) == requirement.RequiredFlags
- Calls context.Succeed(requirement) on match
- Does NOT call context.Fail() — ASP.NET Core returns 403 when no handler succeeds

---

## Files to Modify

### 5. src/CleanArchitecture.Api/Middleware/ExceptionHandlingMiddleware.cs

Add ForbiddenException case in switch expression (before catch-all _):

    ForbiddenException ex => (StatusCodes.Status403Forbidden, new ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Forbidden",
        Detail = ex.Message
    }),

### 6. src/CleanArchitecture.Api/Program.cs

Add after AddInfrastructureServices:

    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

### 7. src/CleanArchitecture.Api/Controllers/UsersController.cs

Add granular permissions alongside existing [Authorize]:

    [HttpGet]
    [RequirePermission(PermissionModule.Users, (long)UserPermissions.Read)]
    public async Task<IActionResult> GetAll(...) { }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionModule.Users, (long)UserPermissions.Read)]
    public async Task<IActionResult> GetById(...) { }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionModule.Users, (long)UserPermissions.Update)]
    public async Task<IActionResult> Update(...) { }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionModule.Users, (long)UserPermissions.Delete)]
    public async Task<IActionResult> Delete(...) { }

---

## Service-Level Check (Optional Pattern)

For real-time (non-JWT) checks, inject IPermissionService directly:

    await _permissionService.RequirePermissionAsync(
        userId, userRole, PermissionModule.Users.ToString(),
        (long)UserPermissions.Delete, ct);

Use when: real-time data needed, runtime-dependent checks, background jobs with no HTTP context.

---

## Verification Checklist

- [ ] [RequirePermission] compiles on controller methods
- [ ] Unauthenticated requests -> 401 (not 403)
- [ ] Missing permission claims -> 403
- [ ] Correct flags -> 200
- [ ] ForbiddenException from services -> 403 ProblemDetails
- [ ] Multiple [RequirePermission] on one method = all must pass (AND logic)
