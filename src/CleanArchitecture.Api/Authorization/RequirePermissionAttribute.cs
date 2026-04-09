using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Api.Authorization;

/// <summary>
/// Requires the caller to have specific permission flags on a module.
/// Uses ASP.NET Core policy-based authorization with dynamic policy names.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public RequirePermissionAttribute(PermissionModule module, long requiredFlags)
    {
        Policy = $"{PolicyPrefix}{module}:{requiredFlags}";
    }
}
