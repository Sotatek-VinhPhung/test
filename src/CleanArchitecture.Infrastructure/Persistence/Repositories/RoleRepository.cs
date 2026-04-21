using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Infrastructure.Persistence;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Role entity.
/// </summary>
public class RoleRepository
{
    private readonly AppDbContext _context;
    
    public RoleRepository(AppDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Get a role by ID with its permissions loaded.
    /// </summary>
    public async Task<Role?> GetByIdWithPermissionsAsync(Guid roleId)
    {
        return await _context.Roles
            .Include(r => r.RoleSubsystemPermissions)
            .ThenInclude(rsp => rsp.Subsystem)
            .FirstOrDefaultAsync(r => r.Id == roleId);
    }
    
    /// <summary>
    /// Get a role by code.
    /// </summary>
    public async Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
    }
    
    /// <summary>
    /// Get all active roles.
    /// </summary>
    public async Task<List<Role>> GetActiveRolesAsync()
    {
        return await _context.Roles
            .Where(r => r.IsActive)
            .ToListAsync();
    }
    
    /// <summary>
    /// Create a new role.
    /// </summary>
    public async Task<Role> CreateAsync(string code, string name, string? description = null)
    {
        var role = new Role
        {
            Code = code,
            Name = name,
            Description = description,
            IsActive = true
        };
        
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        
        return role;
    }
    
    /// <summary>
    /// Update an existing role.
    /// </summary>
    public async Task UpdateAsync(Role role)
    {
        role.UpdatedAt = DateTime.UtcNow;
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Grant permission to a role on a subsystem.
    /// </summary>
    public async Task GrantPermissionAsync(Guid roleId, Guid subsystemId, long flags)
    {
        var existing = await _context.RoleSubsystemPermissions
            .FirstOrDefaultAsync(rsp => rsp.RoleId == roleId && rsp.SubsystemId == subsystemId);
        
        if (existing != null)
        {
            existing.Flags |= flags; // OR existing flags with new flags
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.RoleSubsystemPermissions.Add(new RoleSubsystemPermission
            {
                RoleId = roleId,
                SubsystemId = subsystemId,
                Flags = flags
            });
        }
        
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Revoke permission from a role on a subsystem.
    /// </summary>
    public async Task RevokePermissionAsync(Guid roleId, Guid subsystemId, long flags)
    {
        var permission = await _context.RoleSubsystemPermissions
            .FirstOrDefaultAsync(rsp => rsp.RoleId == roleId && rsp.SubsystemId == subsystemId);
        
        if (permission != null)
        {
            permission.Flags &= ~flags; // Remove flags using bitwise AND NOT
            permission.UpdatedAt = DateTime.UtcNow;
            
            if (permission.Flags == 0)
                _context.RoleSubsystemPermissions.Remove(permission);
            
            await _context.SaveChangesAsync();
        }
    }
}
