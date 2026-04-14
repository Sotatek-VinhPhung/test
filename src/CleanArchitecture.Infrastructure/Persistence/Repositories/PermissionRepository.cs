using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

/// <summary>
/// DEPRECATED: Repository for legacy module-based permissions (RolePermission, UserPermissionOverride).
/// Migrated to new RBAC system using Subsystem, Role, UserRole, RoleSubsystemPermission.
/// 
/// This class is kept for backward compatibility but should not be used in new code.
/// Use RoleRepository, SubsystemRepository instead.
/// </summary>
[Obsolete("Use new RBAC system (RoleRepository, SubsystemRepository) instead of module-based permissions")]
public class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _context;

    public PermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    [Obsolete("Legacy method - not supported in new RBAC")]
    public async Task<IReadOnlyList<RolePermission>> GetByRoleAsync(
        CleanArchitecture.Domain.Enums.Role role, CancellationToken ct = default)
    {
        throw new NotImplementedException("Legacy method not supported. Use new RBAC system.");
    }

    [Obsolete("Legacy method - not supported in new RBAC")]
    public async Task<IReadOnlyList<UserPermissionOverride>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException("Legacy method not supported. Use new RBAC system.");
    }

    [Obsolete("Legacy method - not supported in new RBAC")]
    public async Task<long> GetEffectiveFlagsAsync(
        Guid userId, CleanArchitecture.Domain.Enums.Role role, string module, CancellationToken ct = default)
    {
        throw new NotImplementedException("Legacy method not supported. Use new RBAC system.");
    }

    [Obsolete("Legacy method - not supported in new RBAC")]
    public async Task UpsertRolePermissionAsync(
        RolePermission permission, CancellationToken ct = default)
    {
        throw new NotImplementedException("Legacy method not supported. Use new RBAC system.");
    }

    [Obsolete("Legacy method - not supported in new RBAC")]
    public async Task UpsertUserOverrideAsync(
        UserPermissionOverride overrideEntity, CancellationToken ct = default)
    {
        throw new NotImplementedException("Legacy method not supported. Use new RBAC system.");
    }
}
