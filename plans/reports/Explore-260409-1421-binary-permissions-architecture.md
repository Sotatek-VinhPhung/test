# .NET 8 Clean Architecture Project Exploration
## Binary Permission System Planning

**Date:** 2026-04-09 | **Status:** Complete | **Scope:** Medium

---

## 1. User Entity & Role Enum

### User Entity
**File:** `C:\test\src\CleanArchitecture.Domain\Entities\User.cs`

- Inherits from `BaseEntity` (provides Id GUID, timestamps)
- Single `Role` enum property (not a collection)
- Properties: FirstName, LastName, Email, PasswordHash, Role, RefreshToken, RefreshTokenExpiry
- Email normalized to lowercase at registration

### Role Enum
**File:** `C:\test\src\CleanArchitecture.Domain\Enums\Role.cs`

- Only two values: User (0), Admin (1)
- Hard-coded, not extensible → **Requires refactoring for binary permissions**

---

## 2. Domain Interfaces

### IRepository<T>
**File:** `C:\test\src\CleanArchitecture.Domain\Interfaces\IRepository.cs`

Generic CRUD interface with:
- GetByIdAsync, GetAllAsync, FindAsync, AddAsync, Update, Delete
- Expression-based filtering support

### IUserRepository
**File:** `C:\test\src\CleanArchitecture.Domain\Interfaces\IUserRepository.cs`

Extends IRepository<User> with:
- GetByEmailAsync(string email)

### IUnitOfWork
**File:** `C:\test\src\CleanArchitecture.Domain\Interfaces\IUnitOfWork.cs`

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Extension point:** Can add IPermissionRepository, IRoleRepository

---

## 3. Application Layer Services

### AuthService
**File:** `C:\test\src\CleanArchitecture.Application\Auth\Services\AuthService.cs`

Dependencies:
- IUnitOfWork (repository access)
- IJwtTokenGenerator (token creation)
- IPasswordHasher (bcrypt)

Methods:
```
RegisterAsync(CreateUserRequest) → AuthResponse (access + refresh tokens)
LoginAsync(LoginRequest) → AuthResponse
RefreshAsync(RefreshTokenRequest) → AuthResponse
```

**Token generation call (lines 40, 58, 86):**
```csharp
_jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role.ToString())
```
→ **Can be extended to pass permission flags instead of role string**

### UserService
**File:** `C:\test\src\CleanArchitecture.Application\Users\Services\UserService.cs`

Dependency:
- IUnitOfWork

Methods:
```
GetAllAsync() → List<UserDto>
GetByIdAsync(Guid) → UserDto
UpdateAsync(Guid, UpdateUserRequest) → UserDto
DeleteAsync(Guid) → bool
```

No permission checks yet.

---

## 4. Infrastructure DI Registration

**File:** `C:\test\src\CleanArchitecture.Infrastructure\DependencyInjection.cs`

Registrations:
```
IRepository<>        → Repository<>           (Scoped)
IUserRepository      → UserRepository         (Scoped)
IUnitOfWork          → UnitOfWork             (Scoped)
IJwtTokenGenerator   → JwtTokenGenerator      (Singleton)
IPasswordHasher      → BcryptPasswordHasher   (Singleton)
ICurrentUserService  → CurrentUserService    (Scoped)
```

JWT Bearer authentication configured with:
- Issuer, Audience, Secret validation
- HmacSha256 signature
- ClockSkew = Zero

---

## 5. API Layer

### Program.cs
**File:** `C:\test\src\CleanArchitecture.Api\Program.cs`

Pipeline:
1. ExceptionHandlingMiddleware (custom)
2. Serilog request logging
3. HTTPS redirect
4. Authentication
5. Authorization
6. Controller mapping

Swagger configured with JWT Bearer security scheme.

### AuthController
**File:** `C:\test\src\CleanArchitecture.Api\Controllers\AuthController.cs`

**No [Authorize]** (public endpoints):
- POST /auth/register
- POST /auth/login
- POST /auth/refresh

### UsersController
**File:** `C:\test\src\CleanArchitecture.Api\Controllers\UsersController.cs`

**[Authorize] class-level** (requires JWT):
- GET /users
- GET /users/{id}
- PUT /users/{id}
- DELETE /users/{id}

**Gap:** No granular permission checks like [Authorize(Roles="Admin")]

### ExceptionHandlingMiddleware
**File:** `C:\test\src\CleanArchitecture.Api\Middleware\ExceptionHandlingMiddleware.cs`

Exception handling:
- NotFoundException → 404
- ValidationException (FluentValidation) → 422
- DomainException → 400
- Others → 500

Returns RFC 7807 ProblemDetails JSON.

---

## 6. JWT Token Generation

### JwtTokenGenerator
**File:** `C:\test\src\CleanArchitecture.Infrastructure\Auth\JwtTokenGenerator.cs`

**GenerateAccessToken signature:**
```csharp
public string GenerateAccessToken(Guid userId, string email, string role)
```

**Claims included:**
1. ClaimTypes.NameIdentifier = userId (GUID string)
2. ClaimTypes.Email = email
3. ClaimTypes.Role = role (string: "User" or "Admin")

**Token expiration:** Configured via JwtSettings.AccessTokenExpirationMinutes (default 30)

**RefreshToken generation:**
- 64 random bytes → Base64 string
- Stored in User.RefreshToken
- Expiry tracked in User.RefreshTokenExpiry

**GetPrincipalFromExpiredToken:**
- Validates signature, issuer, audience
- Allows expired tokens (ValidateLifetime = false)
- Used for refresh token flow

### JwtSettings
**File:** `C:\test\src\CleanArchitecture.Infrastructure\Auth\JwtSettings.cs`

```csharp
public class JwtSettings
{
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
```

Bound from appsettings.json["JwtSettings"]

---

## 7. Key Patterns & Extension Points

### Strengths for Permission System
✓ Repository + UnitOfWork pattern → Can add IPermissionRepository  
✓ JWT with claims → Can add permission claims  
✓ Result<T> pattern → Explicit error handling  
✓ Global exception middleware → Consistent error responses  
✓ AuthService uses IUnitOfWork → Can inject additional repositories  

### Gaps for Binary Permissions
✗ Role enum is hard-coded (User, Admin)  
✗ No permission entity or table  
✗ Single Role property per user (not a collection)  
✗ JWT claims include role string only  
✗ No [Authorize(Roles="...")] checks in controllers  
✗ No permission validation middleware  

---

## Design Options for Binary Permissions

### Option A: Binary Flags in User Entity (Simple)
- Add `long Permissions { get; set; }` property
- Use bit positions: (1 << 0) = Read, (1 << 1) = Write, (1 << 2) = Delete, etc.
- Pass as numeric claim in JWT
- Requires: Migration, JWT update, AuthorizePermission attribute

### Option B: Separate Permission Entity (Flexible)
- Create Permission entity (Id, Name, Value)
- Create UserPermission junction table
- Load permissions in JwtTokenGenerator
- Include permission IDs/values in JWT claims
- Requires: 2 new entities, new repository, migration

### Option C: Role + Permissions Entity (Hybrid)
- Keep Role enum for legacy compatibility
- Add Permission entity + UserPermission junction
- User has Role + multiple Permissions
- Roles can have default permissions
- Most flexible long-term

---

## Unresolved Questions

1. How many permission types needed? (typical: 8–64)
2. Permission assignment UI or API only?
3. Should Admin role automatically grant all permissions?
4. Should permissions be cacheable or refreshed on each login?
5. Do permission changes require audit logging?
6. Should permissions be dynamic (added without code changes)?

