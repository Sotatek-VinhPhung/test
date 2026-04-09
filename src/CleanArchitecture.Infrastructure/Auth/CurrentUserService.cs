using System.Security.Claims;
using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CleanArchitecture.Infrastructure.Auth;

public class CurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        UserId = Guid.TryParse(userIdClaim, out var id) ? id : null;
    }
}
