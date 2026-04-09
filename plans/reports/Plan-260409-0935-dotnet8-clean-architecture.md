# Plan: .NET 8 Clean Architecture Web API

**Date:** 2026-04-09
**Status:** Draft
**Working Dir:** C:\test
**Project:** CleanArchitecture (.NET 8, PostgreSQL, JWT, Serilog)

---

## 1. Solution Structure


C:\test+-- CleanArchitecture.sln
+-- .gitignore
+-- .editorconfig
+-- Directory.Build.props                    # Shared build properties
+-- src/
|   +-- CleanArchitecture.Domain/
|   |   +-- CleanArchitecture.Domain.csproj
|   |   +-- Common/
|   |   |   +-- BaseEntity.cs                # Id, CreatedAt, UpdatedAt
|   |   |   +-- BaseEvent.cs                 # Domain event base
|   |   +-- Entities/
|   |   |   +-- User.cs
|   |   +-- Enums/
|   |   |   +-- Role.cs
|   |   +-- Exceptions/
|   |   |   +-- DomainException.cs
|   |   |   +-- NotFoundException.cs
|   |   +-- Interfaces/
|   |       +-- IRepository.cs               # Generic repository interface
|   |       +-- IUserRepository.cs           # User-specific repository
|   |       +-- IUnitOfWork.cs               # Unit of Work interface
|   |
|   +-- CleanArchitecture.Application/
|   |   +-- CleanArchitecture.Application.csproj
|   |   +-- DependencyInjection.cs           # IServiceCollection extensions
|   |   +-- Common/
|   |   |   +-- Interfaces/
|   |   |   |   +-- IJwtTokenGenerator.cs    # JWT token abstraction
|   |   |   |   +-- ICurrentUserService.cs   # Current user from HttpContext
|   |   |   +-- Models/
|   |   |       +-- PagedResult.cs           # Pagination wrapper
|   |   |       +-- Result.cs                # Operation result wrapper
|   |   +-- Users/
|   |   |   +-- DTOs/
|   |   |   |   +-- UserDto.cs
|   |   |   |   +-- CreateUserRequest.cs
|   |   |   |   +-- UpdateUserRequest.cs
|   |   |   |   +-- LoginRequest.cs
|   |   |   +-- Mappings/
|   |   |   |   +-- UserMappings.cs          # Manual extension method mappings
|   |   |   +-- Validators/
|   |   |   |   +-- CreateUserValidator.cs
|   |   |   |   +-- LoginRequestValidator.cs
|   |   |   +-- Services/
|   |   |       +-- IUserService.cs
|   |   |       +-- UserService.cs           # Business logic
|   |   +-- Auth/
|   |       +-- DTOs/
|   |       |   +-- AuthResponse.cs          # Access + Refresh tokens
|   |       |   +-- RefreshTokenRequest.cs
|   |       +-- Services/
|   |           +-- IAuthService.cs
|   |           +-- AuthService.cs
|   |
|   +-- CleanArchitecture.Infrastructure/
|   |   +-- CleanArchitecture.Infrastructure.csproj
|   |   +-- DependencyInjection.cs           # IServiceCollection extensions
|   |   +-- Persistence/
|   |   |   +-- AppDbContext.cs              # EF Core DbContext
|   |   |   +-- Configurations/
|   |   |   |   +-- UserConfiguration.cs     # IEntityTypeConfiguration<User>
|   |   |   +-- Repositories/
|   |   |   |   +-- Repository.cs            # Generic repo implementation
|   |   |   |   +-- UserRepository.cs        # User-specific repo
|   |   |   +-- UnitOfWork.cs
|   |   |   +-- Migrations/                  # EF Core migrations (generated)
|   |   +-- Auth/
|   |       +-- JwtSettings.cs               # JWT config POCO
|   |       +-- JwtTokenGenerator.cs         # Token generation implementation
|   |       +-- CurrentUserService.cs        # ICurrentUserService impl
|   |
|   +-- CleanArchitecture.Api/
|       +-- CleanArchitecture.Api.csproj
|       +-- Program.cs                       # Minimal hosting, DI, pipeline
|       +-- appsettings.json
|       +-- appsettings.Development.json
|       +-- Controllers/
|       |   +-- ApiControllerBase.cs         # [ApiController] base class
|       |   +-- AuthController.cs            # Login, Register, RefreshToken
|       |   +-- UsersController.cs           # CRUD endpoints
|       |   +-- WeatherForecastController.cs # Demo/health-check style
|       +-- Middleware/
|       |   +-- ExceptionHandlingMiddleware.cs
|       +-- Filters/
|           +-- ValidationFilter.cs          # Model validation action filter
|
+-- tests/
    +-- CleanArchitecture.Domain.Tests/
    +-- CleanArchitecture.Application.Tests/
    +-- CleanArchitecture.Api.Tests/


**Total files to create:** ~55 (including .csproj files)
---

## 2. NuGet Packages Per Project

### Domain
None -- pure C# only. Zero external dependencies.

### Application
| Package | Version | Purpose |
|---------|---------|---------|
| FluentValidation | 11.11.* | Request validation |
| FluentValidation.DependencyInjectionExtensions | 11.11.* | DI auto-registration |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.* | IServiceCollection |

### Infrastructure
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0.* | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.* | PostgreSQL provider |
| Microsoft.EntityFrameworkCore.Tools | 8.0.* | Migrations CLI |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.* | JWT middleware |
| System.IdentityModel.Tokens.Jwt | 8.* | Token generation |
| BCrypt.Net-Next | 4.0.* | Password hashing |

### Api
| Package | Version | Purpose |
|---------|---------|---------|
| Serilog.AspNetCore | 8.0.* | Serilog integration |
| Serilog.Sinks.File | 6.0.* | File sink (explicit) |
| Swashbuckle.AspNetCore | 6.* | Swagger/OpenAPI |

### Test Projects (all 3)
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NET.Test.Sdk | 17.* | Test runner |
| xunit | 2.* | Test framework |
| xunit.runner.visualstudio | 2.* | VS integration |
| Moq | 4.* | Mocking |
| FluentAssertions | 7.* | Assertion DSL |

---

## 3. Implementation Phases

### Phase 1: Scaffolding
**Est: ~15 min | 0 code files**

1. dotnet new sln -n CleanArchitecture
2. Create 4 src projects: Domain(classlib), Application(classlib), Infrastructure(classlib), Api(webapi --use-controllers)
3. Create 3 test projects (xunit)
4. dotnet sln add all projects
5. Project references: Application->Domain, Infrastructure->Application, Api->Application+Infrastructure
6. Add NuGet packages per table above
7. Directory.Build.props: Nullable=enable, ImplicitUsings=enable, TreatWarningsAsErrors=true
8. .gitignore (dotnet template), .editorconfig
9. Delete boilerplate (Class1.cs, default WeatherForecast)
10. dotnet build -- verify clean

### Phase 2: Domain Layer
**Est: ~15 min | 9 files**

1. **Common/BaseEntity.cs** -- abstract: Guid Id, DateTime CreatedAt, DateTime UpdatedAt
2. **Common/BaseEvent.cs** -- abstract record (future placeholder)
3. **Entities/User.cs** -- BaseEntity + FirstName, LastName, Email, PasswordHash, Role, RefreshToken?, RefreshTokenExpiry?
4. **Enums/Role.cs** -- enum { User, Admin }
5. **Exceptions/DomainException.cs** -- base domain exception
6. **Exceptions/NotFoundException.cs** -- takes entity name + key
7. **Interfaces/IRepository.cs** -- generic CRUD interface
8. **Interfaces/IUserRepository.cs** -- extends IRepository + GetByEmailAsync
9. **Interfaces/IUnitOfWork.cs** -- SaveChangesAsync + IUserRepository Users

Key pattern -- generic repository interface:



### Phase 3: Application Layer
**Est: ~30 min | 18 files**

1. **Common/Models/Result.cs** -- Result<T>: IsSuccess, Value, Error, static Success/Failure factories
2. **Common/Models/PagedResult.cs** -- Items, TotalCount, Page, PageSize
3. **Common/Interfaces/IJwtTokenGenerator.cs** -- GenerateAccessToken, GenerateRefreshToken, GetPrincipalFromExpiredToken
4. **Common/Interfaces/ICurrentUserService.cs** -- Guid? UserId
5. **Users/DTOs/UserDto.cs** -- record: Id, FirstName, LastName, Email, Role
6. **Users/DTOs/CreateUserRequest.cs** -- record: FirstName, LastName, Email, Password
7. **Users/DTOs/UpdateUserRequest.cs** -- record: FirstName, LastName
8. **Users/DTOs/LoginRequest.cs** -- record: Email, Password
9. **Users/Mappings/UserMappings.cs** -- extension method ToDto() (no AutoMapper)
10. **Users/Validators/CreateUserValidator.cs** -- FluentValidation rules
11. **Users/Validators/LoginRequestValidator.cs**
12. **Users/Services/IUserService.cs** -- GetAll, GetById, Update, Delete
13. **Users/Services/UserService.cs** -- injects IUnitOfWork
14. **Auth/DTOs/AuthResponse.cs** -- AccessToken, RefreshToken, ExpiresAt
15. **Auth/DTOs/RefreshTokenRequest.cs**
16. **Auth/Services/IAuthService.cs** -- Login, Register, Refresh
17. **Auth/Services/AuthService.cs** -- injects IUnitOfWork + IJwtTokenGenerator
18. **DependencyInjection.cs** -- AddApplicationServices()

Key pattern -- manual mapping:



Key pattern -- Result<T>:


### Phase 4: Infrastructure Layer
**Est: ~30 min | 9 files**

1. **Persistence/AppDbContext.cs** -- DbSet<User>, auto-timestamps in SaveChangesAsync, ApplyConfigurationsFromAssembly
2. **Persistence/Configurations/UserConfiguration.cs** -- unique email index, max lengths, Role as string
3. **Persistence/Repositories/Repository.cs** -- generic impl via DbContext.Set<T>()
4. **Persistence/Repositories/UserRepository.cs** -- GetByEmailAsync via LINQ
5. **Persistence/UnitOfWork.cs** -- lazy repo init, delegates SaveChangesAsync
6. **Auth/JwtSettings.cs** -- POCO: Secret, Issuer, Audience, AccessTokenExpirationMinutes, RefreshTokenExpirationDays
7. **Auth/JwtTokenGenerator.cs** -- HMAC-SHA256, claims: sub/email/role
8. **Auth/CurrentUserService.cs** -- reads ClaimTypes.NameIdentifier via IHttpContextAccessor
9. **DependencyInjection.cs** -- AddInfrastructureServices(IConfiguration): DbContext, repos, UoW, JWT auth

Key pattern -- auto-timestamps in SaveChangesAsync override:
- Loop ChangeTracker.Entries<BaseEntity>()
- EntityState.Added -> set CreatedAt = DateTime.UtcNow
- EntityState.Modified -> set UpdatedAt = DateTime.UtcNow

### Phase 5: API Layer
**Est: ~30 min | 9 files**

1. **appsettings.json** -- ConnectionStrings (PostgreSQL), JwtSettings (Secret, Issuer, Audience, Expirations), Serilog (Console + File daily rolling)
2. **appsettings.Development.json** -- Serilog MinimumLevel=Debug
3. **Program.cs** -- Serilog via builder.Host.UseSerilog, AddApplicationServices(), AddInfrastructureServices(config), AddControllers, Swagger+JWT SecurityDefinition, middleware pipeline
4. **Controllers/ApiControllerBase.cs** -- [ApiController][Route(api/[controller])] abstract ControllerBase
5. **Controllers/AuthController.cs** -- POST register(201), POST login(200), POST refresh(200)
6. **Controllers/UsersController.cs** -- [Authorize] GET list(200), GET {id}(200), PUT {id}(200), DELETE {id}(204)
7. **Controllers/WeatherForecastController.cs** -- demo, no auth required
8. **Middleware/ExceptionHandlingMiddleware.cs** -- ProblemDetails (RFC 7807): NotFoundException->404, DomainException->400, ValidationException->422, unhandled->500
9. **Filters/ValidationFilter.cs** -- IActionFilter for custom validation

Middleware pipeline order:
1. ExceptionHandlingMiddleware
2. UseSerilogRequestLogging()
3. UseSwagger/UseSwaggerUI (dev only)
4. UseAuthentication()
5. UseAuthorization()
6. MapControllers()

### Phase 6: Database Migration + Smoke Test
**Est: ~10 min | 0 new code files**

1. Ensure PostgreSQL running
2. dotnet ef migrations add InitialCreate -p src/CleanArchitecture.Infrastructure -s src/CleanArchitecture.Api
3. dotnet ef database update -p src/CleanArchitecture.Infrastructure -s src/CleanArchitecture.Api
4. dotnet run --project src/CleanArchitecture.Api
5. Verify: Swagger at /swagger, register, login returns JWT, authorized CRUD works, WeatherForecast unauthenticated

### Phase 7: Test Scaffolding
**Est: ~15 min | 6 files**

1. **Domain.Tests**: User entity creation tests
2. **Application.Tests**: UserService with mocked IUnitOfWork (Moq)
3. **Api.Tests**: WebApplicationFactory<Program> integration tests

---

## 4. Key Design Patterns

### 4.1 Dependency Flow (strict)
- Api --> Application --> Domain
- Api --> Infrastructure --> Application --> Domain
- Domain: ZERO outward deps
- Infrastructure implements interfaces defined in Domain/Application

### 4.2 DI Registration Per Layer
Each layer: one static DependencyInjection class, one IServiceCollection extension method.
- Application: AddApplicationServices(this IServiceCollection)
- Infrastructure: AddInfrastructureServices(this IServiceCollection, IConfiguration)
- Api calls both in Program.cs

### 4.3 Thin Controllers
Delegate to Application services, return IActionResult. Zero business logic in controllers.

### 4.4 No MediatR / No CQRS
KISS: direct service injection. Refactor to Commands/Queries later if CQRS needed.

### 4.5 No AutoMapper
Manual mapping extension methods. Compile-time safe, zero magic. Sufficient for starter.

### 4.6 Password Hashing
BCrypt.Net-Next: BCrypt.HashPassword() / BCrypt.Verify(). Stored in User.PasswordHash.

### 4.7 Refresh Token Strategy
- Refresh token = random Base64 string stored on User entity
- Login: generate access + refresh tokens, persist refresh to DB
- Refresh: validate expired access token, match refresh in DB, issue new pair
- Logout (future): clear refresh token in DB

---

## 5. Timeline Summary

| Phase | Layer | Time | Depends On |
|-------|-------|------|------------|
| 1 | Scaffolding | 15 min | -- |
| 2 | Domain | 15 min | P1 |
| 3 | Application | 30 min | P2 |
| 4 | Infrastructure | 30 min | P3 |
| 5 | API | 30 min | P4 |
| 6 | Migration + Smoke | 10 min | P5+PostgreSQL |
| 7 | Tests | 15 min | P5 |
| **Total** | | **~2.5 hrs** | |

---

## 6. Decisions Log

| Decision | Choice | Why |
|----------|--------|-----|
| Mediator | No (direct services) | KISS |
| Mapping | No (manual extensions) | ~3 DTOs |
| Validation | FluentValidation | Industry standard |
| Auth | JWT + refresh tokens | Stateless REST |
| Passwords | BCrypt.Net-Next | Battle-tested |
| Logging | Serilog Console+File | .NET de facto |
| DB | Npgsql EF Core 8 | PostgreSQL |
| Tests | xUnit+Moq+FluentAssertions | Popular |
| Errors | Middleware+ProblemDetails | RFC 7807 |

---

## 7. Unresolved Questions

None. All requirements clear. Ready for implementation.