using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Infrastructure.Persistence;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Subsystem entity.
/// </summary>
public class SubsystemRepository
{
    private readonly AppDbContext _context;
    
    public SubsystemRepository(AppDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Get a subsystem by ID.
    /// </summary>
    public async Task<Subsystem?> GetByIdAsync(Guid id)
    {
        return await _context.Subsystems.FirstOrDefaultAsync(s => s.Id == id);
    }
    
    /// <summary>
    /// Get a subsystem by code (e.g., "Reports", "Users").
    /// </summary>
    public async Task<Subsystem?> GetByCodeAsync(string code)
    {
        return await _context.Subsystems
            .FirstOrDefaultAsync(s => s.Code == code);
    }
    
    /// <summary>
    /// Get all active subsystems.
    /// </summary>
    public async Task<List<Subsystem>> GetActiveSubsystemsAsync()
    {
        return await _context.Subsystems
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
    
    /// <summary>
    /// Get all subsystems (including inactive).
    /// </summary>
    public async Task<List<Subsystem>> GetAllAsync()
    {
        return await _context.Subsystems
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
    
    /// <summary>
    /// Create a new subsystem.
    /// </summary>
    public async Task<Subsystem> CreateAsync(string code, string name, string? description = null)
    {
        var subsystem = new Subsystem
        {
            Code = code,
            Name = name,
            Description = description,
            IsActive = true
        };
        
        _context.Subsystems.Add(subsystem);
        await _context.SaveChangesAsync();
        
        return subsystem;
    }
    
    /// <summary>
    /// Update an existing subsystem.
    /// </summary>
    public async Task UpdateAsync(Subsystem subsystem)
    {
        _context.Subsystems.Update(subsystem);
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Get all role permissions for a subsystem.
    /// </summary>
    public async Task<List<RoleSubsystemPermission>> GetRolePermissionsAsync(Guid subsystemId)
    {
        return await _context.RoleSubsystemPermissions
            .Where(rsp => rsp.SubsystemId == subsystemId)
            .Include(rsp => rsp.Role)
            .ToListAsync();
    }
}
