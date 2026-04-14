using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Application.Permissions.Services;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Services;

/// <summary>
/// Infrastructure implementation of UserContextService.
/// Loads user context from the database and merges permissions.
/// </summary>
public class UserContextServiceImpl : UserContextServiceBase
{
    private readonly AppDbContext _context;
    
    public UserContextServiceImpl(AppDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Load UserContext for a user by loading all roles and merging their permissions.
    /// </summary>
    public override async Task<UserContext?> GetUserContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RoleSubsystemPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return null;
        
        return await BuildUserContextAsync(user, cancellationToken);
    }
    
    /// <summary>
    /// Build UserContext from user data by merging role permissions.
    /// Uses bitwise OR to combine permissions from all roles.
    /// </summary>
    private async Task<UserContext> BuildUserContextAsync(User user, CancellationToken cancellationToken = default)
    {
        // Get all active role IDs
        var activeRoleIds = user.UserRoles
            .Where(ur => ur.IsActive())
            .Select(ur => ur.RoleId)
            .ToList();
        
        // If no active roles, return empty context
        if (!activeRoleIds.Any())
        {
            return UserContext.Create(
                user.Id,
                user.Email,
                [],
                new Dictionary<string, long>());
        }
        
        // Load all role permissions for this user across all subsystems
        var rolePermissions = await _context.RoleSubsystemPermissions
            .Where(rsp => activeRoleIds.Contains(rsp.RoleId))
            .Select(rsp => new { rsp.Subsystem.Code, rsp.Flags })
            .ToListAsync(cancellationToken);
        
        // Merge permissions by subsystem using bitwise OR
        var subsystemPermissions = new Dictionary<string, long>();
        foreach (var rp in rolePermissions)
        {
            if (subsystemPermissions.TryGetValue(rp.Code, out var existing))
                subsystemPermissions[rp.Code] = existing | rp.Flags; // Bitwise OR to merge
            else
                subsystemPermissions[rp.Code] = rp.Flags;
        }
        
        // Create and return the immutable UserContext
        return UserContext.Create(
            user.Id,
            user.Email,
            activeRoleIds,
            subsystemPermissions);
    }
}
