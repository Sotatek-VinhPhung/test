# Plans & Reports Index

## 2026-04-09

### Plan-260409-1441-binary-permissions.md
**Status:** Ready for implementation  
**Type:** Implementation Plan (5 phases)  
**Purpose:** Binary/bitwise permission system for CleanArchitecture project

**Contains:**
- Main plan.md with architecture overview, phase summary, design decisions
- phase-1-domain.md: [Flags] enums, RolePermission/UserPermissionOverride entities
- phase-2-infrastructure.md: EF configs, PermissionRepository, migration
- phase-3-application.md: PermissionService, JWT permission claims
- phase-4-api.md: [RequirePermission] attribute + authorization handler pipeline
- phase-5-seed.md: Default role permissions seeder

**Key Stats:** 21 files to create, 9 to modify, 5 phases
**Plan Location:** /c/test/plans/260409-1416-binary-permissions/

---

### Explore-260409-1421-binary-permissions-architecture.md
**Status:** Complete  
**Type:** Exploration Report  
**Purpose:** Understand Clean Architecture project structure for planning binary permission system

**Contains:**
1. Current User entity and Role enum (code samples)
2. Domain layer interfaces (IRepository, IUserRepository, IUnitOfWork)
3. Application layer services (AuthService, UserService) with IUnitOfWork usage
4. Infrastructure DI registration (DependencyInjection.cs)
5. API layer (Program.cs, Controllers, ExceptionHandlingMiddleware)
6. JWT token generation (JwtTokenGenerator, claims included)
7. Project structure map
8. Key patterns and extension points
9. Design options (A, B, C)
10. Unresolved questions

**Key Findings:**
- Architecture is clean and ready for permission system addition
- Recommended approach: Option C (Role + Permissions Entity Hybrid)
- Extension points identified in all layers

---

### Plan-260409-0935-dotnet8-clean-architecture.md
**Status:** Complete (implemented)  
**Type:** Implementation Plan  
**Purpose:** Initial .NET 8 Clean Architecture project scaffold

---

## Report Location
All reports: /c/test/plans/reports/
All plans: /c/test/plans/
