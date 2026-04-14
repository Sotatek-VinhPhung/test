using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Represents a functional subsystem in the application.
/// Examples: Reports, Users, Analytics, Settings, Audit.
/// Each subsystem has its own set of permissions managed via RoleSubsystemPermissions.
/// </summary>
public class Subsystem : BaseEntity
{
    /// <summary>
    /// Unique code for the subsystem (e.g., "Reports", "Users", "Analytics").
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name for the subsystem.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the subsystem's purpose.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Indicates if this subsystem is active/enabled.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Navigation property for role permissions on this subsystem.
    /// </summary>
    public ICollection<RoleSubsystemPermission> RoleSubsystemPermissions { get; set; } = [];
    
    /// <summary>
    /// Get all subsystems as a well-known collection.
    /// </summary>
    public static class WellKnown
    {
        public const string Reports = "Reports";
        public const string Users = "Users";
        public const string Analytics = "Analytics";
        public const string Settings = "Settings";
        public const string Audit = "Audit";
    }
}
