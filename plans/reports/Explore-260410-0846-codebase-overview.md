# Codebase Exploration Report

## Project Structure
**Solution:** CleanArchitecture.slnx (4 src + 3 test projects)

### Core Layers
- **CleanArchitecture.Domain**: Entity definitions, no external deps
- **CleanArchitecture.Application**: Services, validators (FluentValidation), DTOs, DI extension
- **CleanArchitecture.Infrastructure**: EF Core, repositories, auth, PostgreSQL
- **CleanArchitecture.Api**: ASP.NET Core 8 web API, controllers, middleware, authorization

## Project Files
```
src/
├── CleanArchitecture.Api/
│   ├── Program.cs (DI registration, middleware pipeline, Serilog config)
│   ├── Controllers/ (AuthController, UsersController)
│   ├── Authorization/ (PermissionAuthorizationHandler, RequirePermissionAttribute)
│   └── Middleware/ (ExceptionHandlingMiddleware)
├── CleanArchitecture.Application/
│   ├── DependencyInjection.cs (service registration)
│   ├── Auth/ (AuthService, DTOs)
│   └── Users/ (UserService, validators)
├── CleanArchitecture.Infrastructure/
│   ├── DependencyInjection.cs (DB, auth, JWT, repos)
│   ├── Persistence/ (AppDbContext, repositories, UoW)
│   └── Auth/ (JwtTokenGenerator, BcryptPasswordHasher, CurrentUserService)
└── CleanArchitecture.Domain/
    └── (Entities, interfaces)
tests/
├── CleanArchitecture.Api.Tests/
├── CleanArchitecture.Application.Tests/
└── CleanArchitecture.Domain.Tests/
```

## Configuration
- **appsettings.json**: ConnectionStrings (PostgreSQL), JwtSettings, Serilog (console/file)
- **Directory.Build.props**: Centralized build properties

## NuGet Packages
- **Logging**: Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0)
- **API**: Swashbuckle.AspNetCore (6.6.2)
- **ORM**: Microsoft.EntityFrameworkCore (8.0.*), Npgsql.EntityFrameworkCore.PostgreSQL (8.0.*)
- **Auth**: Microsoft.AspNetCore.Authentication.JwtBearer (8.0.*), BCrypt.Net-Next (4.1.0)
- **Validation**: FluentValidation (12.1.1)

## DI Patterns
- **Extension methods** (AddApplicationServices, AddInfrastructureServices) in DependencyInjection.cs
- **Repository pattern** (IRepository<T>, UserRepository, PermissionRepository)
- **Unit of Work** (IUnitOfWork)
- **Scoped services**: UserService, AuthService, PermissionService, CurrentUserService
- **Singleton services**: JwtTokenGenerator, PasswordHasher, AuthorizationPolicyProvider

## Messaging/Kafka
**None found.** No Kafka, RabbitMQ, or event bus references.

## Key Observations
- Clean Architecture with clear layer separation
- JWT-based authentication with permission-based authorization
- PostgreSQL with EF Core (migrations ready)
- Comprehensive error handling middleware
- No external messaging integration yet
