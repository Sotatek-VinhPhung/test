using CleanArchitecture.Application.Auth.DTOs;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Users.DTOs;

namespace CleanArchitecture.Application.Auth.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
