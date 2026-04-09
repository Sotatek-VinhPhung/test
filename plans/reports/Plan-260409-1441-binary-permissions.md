# Plan Report: Binary/Bitwise Permission System

**Date**: 2026-04-09
**Plan**: C:\test\plans\260409-1416-binary-permissions\plan.md
**Status**: Ready for implementation

## Summary

5-phase plan to add a binary/bitwise permission system to the .NET 8 Clean Architecture project. Module-based [Flags] enums stored as long columns, with Role defaults + per-user overrides combined via bitwise OR.

## Phases

| # | Name | Create | Modify | Key Deliverable |
|---|------|--------|--------|-----------------|
| 1 | Domain Model | 7 | 0 | [Flags] enums, RolePermission/UserPermissionOverride entities |
| 2 | Infrastructure | 5 | 3 | EF configs, PermissionRepository, migration |
| 3 | Application | 3 | 3 | PermissionService, JWT permission claims |
| 4 | API Authorization | 4 | 2 | [RequirePermission] attribute + handler pipeline |
| 5 | Seed Data | 2 | 1 | Default role permissions, startup seeder |

**Total**: 21 files created, 9 modified

## Key Decisions

- **Storage**: long (bigint) per module, string module names -> no migration for new modules
- **Resolution**: Effective = RoleFlags | UserOverrideFlags (additive only, no deny)
- **JWT**: Permission claims embedded as perm:{Module}={flags} -> zero DB calls for attribute checks
- **Dual enforcement**: [RequirePermission] attribute (JWT-based) + IPermissionService (DB-based)
- **Backward compatible**: Existing Role enum and auth flow unchanged

## Unresolved Questions

1. **Cache invalidation**: Stale JWT permissions between refreshes. Recommend short TTL (5-15 min) for v1.
2. **Deny flags**: Not implemented. Additive-only is simpler and sufficient for v1.
3. **Permission management API**: CRUD endpoints for permission assignment not in scope. Follow-up task.

## Files

- [plan.md](../260409-1416-binary-permissions/plan.md)
- [phase-1-domain.md](../260409-1416-binary-permissions/phase-1-domain.md)
- [phase-2-infrastructure.md](../260409-1416-binary-permissions/phase-2-infrastructure.md)
- [phase-3-application.md](../260409-1416-binary-permissions/phase-3-application.md)
- [phase-4-api.md](../260409-1416-binary-permissions/phase-4-api.md)
- [phase-5-seed.md](../260409-1416-binary-permissions/phase-5-seed.md)
