using CleanArchitecture.Application.Permissions;
using CleanArchitecture.Application.Permissions.Interfaces;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Infrastructure.Persistence;

namespace CleanArchitecture.Api.Authorization;

/// <summary>
/// Checks user permissions with 3-tier model:
/// Tier 1: RBAC (Role-Based Access Control) - subsystem + permissions
/// Tier 2: ABAC (Attribute-Based Access Control) - organization scope (region/company/department)
/// Tier 3: Entity-level (optional) - per-resource restrictions
///
/// Extracts user ID from JWT claims, then uses IHierarchicalPermissionService to verify permissions
/// including organizational scope restrictions.
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

        try
        {
            var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
            var hierarchicalPermissionService = _serviceProvider.GetRequiredService<IHierarchicalPermissionService>();

            // Get subsystem by code
            var subsystem = await dbContext.Subsystems
                .FirstOrDefaultAsync(s => s.Code == requirement.Module);

            if (subsystem == null)
            {
                // Subsystem not found - fail
                return;
            }

            // Parse permission flags from requirement
            var permission = (Permission)requirement.RequiredFlags;

            // ✅ TIER 1+2: Check RBAC + ABAC (Organizational Scope)
            // This automatically checks:
            // - User has role with permission on subsystem (RBAC)
            // - User's role is scoped to their organization level (ABAC)
            var hasPermissionInUserScope = await hierarchicalPermissionService.HasPermissionInUserScopeAsync(
                userId,
                subsystem.Id,
                permission,
                CancellationToken.None
            );

            if (hasPermissionInUserScope)
            {
                context.Succeed(requirement);
                return;
            }

            // ✅ TIER 3: Check for target scope override (optional)
            // If request specifies target organization scope, verify user can access that scope
            var targetRegionId = ExtractClaimAsGuid(context, "target_region_id");
            var targetCompanyId = ExtractClaimAsGuid(context, "target_company_id");
            var targetDepartmentId = ExtractClaimAsGuid(context, "target_department_id");

            if (targetRegionId.HasValue || targetCompanyId.HasValue || targetDepartmentId.HasValue)
            {
                var hasPermissionInTargetScope = await hierarchicalPermissionService.HasPermissionInScopeAsync(
                    userId,
                    subsystem.Id,
                    permission,
                    targetRegionId,
                    targetCompanyId,
                    targetDepartmentId,
                    CancellationToken.None
                );

                if (hasPermissionInTargetScope)
                {
                    context.Succeed(requirement);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission check error: {ex.Message}");
            // Fail silently - will result in 403
        }
    }

    /// <summary>
    /// Helper: Extract Guid claim value
    /// </summary>
    private Guid? ExtractClaimAsGuid(AuthorizationHandlerContext context, string claimType)
    {
        var value = context.User.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
