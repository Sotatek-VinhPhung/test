using System.Security.Claims;

namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// JWT token generation and validation abstraction.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email, string role,
        Dictionary<string, long>? permissions = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
