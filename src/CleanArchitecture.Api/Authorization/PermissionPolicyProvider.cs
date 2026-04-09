using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Api.Authorization;

/// <summary>
/// Dynamically creates authorization policies from "Permission:{Module}:{Flags}" policy names.
/// Falls back to default provider for standard policies.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix))
            return _fallback.GetPolicyAsync(policyName);

        // Parse "Permission:Users:15" → module="Users", flags=15
        var parts = policyName[RequirePermissionAttribute.PolicyPrefix.Length..].Split(':');
        if (parts.Length != 2 || !long.TryParse(parts[1], out var flags))
            return _fallback.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(parts[0], flags))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();
}
