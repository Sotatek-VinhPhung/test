using CleanArchitecture.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Api.Authorization;

/// <summary>
/// Checks JWT permission claims against the required flags using bitwise AND.
/// Does NOT call context.Fail() — ASP.NET Core returns 403 when no handler succeeds.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var claimValue = context.User.FindFirst(
            PermissionClaimNames.ForModule(requirement.Module))?.Value;

        if (claimValue is not null && long.TryParse(claimValue, out var userFlags))
        {
            if ((userFlags & requirement.RequiredFlags) == requirement.RequiredFlags)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
