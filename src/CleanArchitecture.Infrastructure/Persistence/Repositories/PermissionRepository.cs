using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _context;

    public PermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RolePermission>> GetByRoleAsync(
        Role role, CancellationToken ct = default)
    {
        return await _context.RolePermissions
            .Where(rp => rp.Role == role)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserPermissionOverride>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await _context.UserPermissionOverrides
            .Where(up => up.UserId == userId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<long> GetEffectiveFlagsAsync(
        Guid userId, Role role, string module, CancellationToken ct = default)
    {
        var roleFlags = await _context.RolePermissions
            .Where(rp => rp.Role == role && rp.Module == module)
            .Select(rp => rp.Flags)
            .FirstOrDefaultAsync(ct);

        var userFlags = await _context.UserPermissionOverrides
            .Where(up => up.UserId == userId && up.Module == module)
            .Select(up => up.Flags)
            .FirstOrDefaultAsync(ct);

        return roleFlags | userFlags;
    }

    public async Task UpsertRolePermissionAsync(
        RolePermission permission, CancellationToken ct = default)
    {
        var existing = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == permission.Role
                && rp.Module == permission.Module, ct);

        if (existing is null)
            await _context.RolePermissions.AddAsync(permission, ct);
        else
            existing.Flags = permission.Flags;
    }

    public async Task UpsertUserOverrideAsync(
        UserPermissionOverride overrideEntity, CancellationToken ct = default)
    {
        var existing = await _context.UserPermissionOverrides
            .FirstOrDefaultAsync(up => up.UserId == overrideEntity.UserId
                && up.Module == overrideEntity.Module, ct);

        if (existing is null)
            await _context.UserPermissionOverrides.AddAsync(overrideEntity, ct);
        else
            existing.Flags = overrideEntity.Flags;
    }
}
