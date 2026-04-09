namespace CleanArchitecture.Infrastructure.Auth;

/// <summary>
/// Constants for permission-related JWT claim names.
/// Format: "perm:{Module}" with value = flags as string.
/// </summary>
public static class PermissionClaimNames
{
    public const string Prefix = "perm:";

    public static string ForModule(string module) => $"{Prefix}{module}";
}
