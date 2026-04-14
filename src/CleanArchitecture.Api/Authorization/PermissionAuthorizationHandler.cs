using CleanArchitecture.Application.Permissions;
using CleanArchitecture.Application.Permissions.Interfaces;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Api.Authorization;

/// <summary>
/// Checks user permissions against role subsystem permissions using new RBAC.
/// Extracts user ID from JWT claims, then uses IPermissionService to verify permissions.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Get user ID from JWT claims
        var userIdClaim = context.User.FindFirst("sub")?.Value ?? 
                         context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            // No valid user ID in claims - fail silently (will result in 403)
            return;
        }

        // Get permission service
        var permissionService = _serviceProvider.GetRequiredService<IPermissionService>();

        try
        {
            // Check if user has required permission in subsystem
            var hasPermission = await permissionService.HasPermissionAsync(
                userId,
                Role.Admin, // role parameter is now legacy - RBAC uses UserRole junction table
                requirement.Module, // subsystem code
                requirement.RequiredFlags,
                CancellationToken.None
            );

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission check error: {ex.Message}");
            // Fail silently - will result in 403
        }
    }
}
