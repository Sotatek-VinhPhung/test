using CleanArchitecture.Application.Permissions.Interfaces;
using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of IRolePermissionRepository using new RBAC system.
/// Queries RoleSubsystemPermission, UserRole, Subsystem entities.
/// </summary>
public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly AppDbContext _context;

    public RolePermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Subsystem?> GetSubsystemByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _context.Subsystems
            .FirstOrDefaultAsync(s => s.Code == code, ct);
    }

    public async Task<List<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid subsystemId, long requiredFlags, CancellationToken ct = default)
    {
        // Get user's role IDs
        var userRoleIds = await GetUserRoleIdsAsync(userId, ct);

        if (!userRoleIds.Any())
            return false;

        // Check if any user role has required permission in subsystem
        return await _context.RoleSubsystemPermissions
            .Where(rsp => rsp.SubsystemId == subsystemId && userRoleIds.Contains(rsp.RoleId))
            .AnyAsync(rsp => (rsp.Flags & requiredFlags) == requiredFlags, ct);
    }

    public async Task<Dictionary<string, long>> GetAllEffectivePermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        // Get user's role IDs
        var userRoleIds = await GetUserRoleIdsAsync(userId, ct);

        if (!userRoleIds.Any())
            return new Dictionary<string, long>();

        // Get all permissions for user's roles grouped by subsystem code
        // Combine flags using bitwise OR to get effective permissions
        var permissions = await _context.RoleSubsystemPermissions
            .Where(rsp => userRoleIds.Contains(rsp.RoleId))
            .Include(rsp => rsp.Subsystem)
            .GroupBy(rsp => rsp.Subsystem.Code)
            .Select(g => new { Code = g.Key, CombinedFlags = g.Aggregate(0L, (acc, rsp) => acc | rsp.Flags) })
            .ToDictionaryAsync(x => x.Code, x => x.CombinedFlags, ct);

        return permissions;
    }
}
