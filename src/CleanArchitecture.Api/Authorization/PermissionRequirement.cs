using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Api.Authorization;

/// <summary>
/// Authorization requirement specifying a module and required permission flags.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Module { get; }
    public long RequiredFlags { get; }

    public PermissionRequirement(string module, long requiredFlags)
    {
        Module = module;
        RequiredFlags = requiredFlags;
    }
}
