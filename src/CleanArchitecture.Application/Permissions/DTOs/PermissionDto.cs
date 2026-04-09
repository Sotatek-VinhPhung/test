namespace CleanArchitecture.Application.Permissions.DTOs;

/// <summary>
/// Represents a user's effective permissions for one module.
/// </summary>
public record PermissionDto(string Module, long Flags);
