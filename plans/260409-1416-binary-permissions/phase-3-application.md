# Phase 3: Application — Permission Service & JWT Integration

> **Depends on**: Phase 2
> **Blocks**: Phase 4

## Goal
Implement the permission resolution service and wire permissions into JWT token generation during login/register/refresh.

---

## Files to Create

### 1. `src/CleanArchitecture.Application/Permissions/IPermissionService.cs`

```csharp
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Permissions;

/// <summary>
/// Service for checking and resolving permissions.
/// Use this for service-level permission checks (hits DB for real-time data).
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if a user has ALL the required flags on a module.
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid userId, Role role, string module, long requiredFlags,
        CancellationToken ct = default);

    /// <summary>
    /// Get the effective (Role | Override) flags for all modules for a user.
    /// Returns a dictionary: module name → effective flags.
    /// Used when building JWT claims.
    /// </summary>
    Task<Dictionary<string, long>> GetAllEffectiveAsync(
        Guid userId, Role role, CancellationToken ct = default);

    /// <summary>
    /// Throws ForbiddenException if the user lacks required flags.
    /// Convenience wrapper around HasPermissionAsync.
    /// </summary>
    Task RequirePermissionAsync(
        Guid userId, Role role, string module, long requiredFlags,
        CancellationToken ct = default);
}
```

---

### 2. `src/CleanArchitecture.Application/Permissions/PermissionService.cs`

```csharp
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;

    public PermissionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId, Role role, string module, long requiredFlags,
        CancellationToken ct = default)
    {
        var effective = await _unitOfWork.Permissions
            .GetEffectiveFlagsAsync(userId, role, module, ct);

        // Bitwise check: user has ALL required flags
        return (effective & requiredFlags) == requiredFlags;
    }

    public async Task<Dictionary<string, long>> GetAllEffectiveAsync(
        Guid userId, Role role, CancellationToken ct = default)
    {
        var rolePerms = await _unitOfWork.Permissions.GetByRoleAsync(role, ct);
        var userOverrides = await _unitOfWork.Permissions.GetByUserIdAsync(userId, ct);

        // Start with role defaults
        var result = rolePerms.ToDictionary(rp => rp.Module, rp => rp.Flags);

        // OR in user overrides
        foreach (var uo in userOverrides)
        {
            if (result.TryGetValue(uo.Module, out var existing))
                result[uo.Module] = existing | uo.Flags;
            else
                result[uo.Module] = uo.Flags;
        }

        return result;
    }

    public async Task RequirePermissionAsync(
        Guid userId, Role role, string module, long requiredFlags,
        CancellationToken ct = default)
    {
        if (!await HasPermissionAsync(userId, role, module, requiredFlags, ct))
            throw new ForbiddenException(module, requiredFlags);
    }
}
```

**Key bitwise logic**:
```
effective = roleFlags | userOverrideFlags    // combine
hasAll    = (effective & required) == required // check
```

---

### 3. `src/CleanArchitecture.Application/Permissions/DTOs/PermissionDto.cs`

```csharp
namespace CleanArchitecture.Application.Permissions.DTOs;

/// <summary>
/// Represents a user's effective permissions for one module.
/// </summary>
public record PermissionDto(string Module, long Flags);
```

---

## Files to Modify

### 4. `src/CleanArchitecture.Application/Common/Interfaces/IJwtTokenGenerator.cs`

**Change** the `GenerateAccessToken` signature to accept permissions:

```csharp
// BEFORE:
string GenerateAccessToken(Guid userId, string email, string role);

// AFTER:
string GenerateAccessToken(
    Guid userId, string email, string role,
    Dictionary<string, long>? permissions = null);
```

The `permissions` parameter is nullable with a default — this keeps backward compatibility during incremental migration. The `JwtTokenGenerator` implementation (Infrastructure) will add `perm:Module=flags` claims when permissions are provided.

**Also update `JwtTokenGenerator.cs`** in Infrastructure:

```csharp
public string GenerateAccessToken(
    Guid userId, string email, string role,
    Dictionary<string, long>? permissions = null)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new(ClaimTypes.Email, email),
        new(ClaimTypes.Role, role)
    };

    // Add permission claims
    if (permissions is not null)
    {
        foreach (var (module, flags) in permissions)
        {
            claims.Add(new Claim(
                PermissionClaimNames.ForModule(module),
                flags.ToString()));
        }
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _settings.Issuer,
        audience: _settings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

### 5. `src/CleanArchitecture.Application/Auth/Services/AuthService.cs`

**Inject** `IPermissionService` and load permissions before generating tokens.

Changes in constructor:
```csharp
private readonly IPermissionService _permissionService;

public AuthService(
    IUnitOfWork unitOfWork,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher,
    IPermissionService permissionService)
{
    _unitOfWork = unitOfWork;
    _jwtTokenGenerator = jwtTokenGenerator;
    _passwordHasher = passwordHasher;
    _permissionService = permissionService;
}
```

**Extract** a helper method to avoid repeating permission-loading in Register/Login/Refresh:
```csharp
private async Task<string> GenerateAccessTokenWithPermissions(
    User user, CancellationToken ct)
{
    var permissions = await _permissionService
        .GetAllEffectiveAsync(user.Id, user.Role, ct);

    return _jwtTokenGenerator.GenerateAccessToken(
        user.Id, user.Email, user.Role.ToString(), permissions);
}
```

Then replace all three occurrences of `_jwtTokenGenerator.GenerateAccessToken(...)` with:
```csharp
var accessToken = await GenerateAccessTokenWithPermissions(user, cancellationToken);
```

---

### 6. `src/CleanArchitecture.Application/DependencyInjection.cs`

**Add** after the `IAuthService` registration:
```csharp
services.AddScoped<IPermissionService, PermissionService>();
```

---

## Verification Checklist
- [ ] `PermissionService` uses `IUnitOfWork.Permissions` (not direct repo injection)
- [ ] JWT now contains `perm:Users=15` style claims after login
- [ ] Existing auth flow still works (permissions param is optional)
- [ ] `GetAllEffectiveAsync` correctly ORs role + user overrides
- [ ] No circular dependencies
