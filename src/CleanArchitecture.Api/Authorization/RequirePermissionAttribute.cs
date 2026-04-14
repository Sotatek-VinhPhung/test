using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Api.Authorization;

/// <summary>
/// Requires the caller to have specific permission flags on a subsystem.
/// Uses ASP.NET Core policy-based authorization with dynamic policy names.
/// Updated to support new RBAC (Subsystem-based) system.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    /// <summary>
    /// Create permission requirement for a subsystem with required flags.
    /// </summary>
    public RequirePermissionAttribute(string subsystemCode, long requiredFlags)
    {
        // Map old PermissionModule to new Subsystem codes if needed
        var mappedCode = MapLegacyModule(subsystemCode);
        Policy = $"{PolicyPrefix}{mappedCode}:{requiredFlags}";
    }

    /// <summary>
    /// Legacy: Support old PermissionModule enum for backward compatibility.
    /// </summary>
    [Obsolete("Use subsystem code string instead of PermissionModule enum")]
    public RequirePermissionAttribute(PermissionModule module, long requiredFlags)
    {
        // Map old module to new subsystem code
        var subsystemCode = module switch
        {
            PermissionModule.Users => "Users",
            PermissionModule.Orders => "Orders",
            PermissionModule.Settings => "Settings",
            _ => module.ToString()
        };

        Policy = $"{PolicyPrefix}{subsystemCode}:{requiredFlags}";
    }

    private static string MapLegacyModule(string code)
    {
        // If already a subsystem code, return as-is
        return code;
    }
}
