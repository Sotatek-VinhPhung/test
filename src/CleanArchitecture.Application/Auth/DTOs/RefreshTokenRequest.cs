namespace CleanArchitecture.Application.Auth.DTOs;

public record RefreshTokenRequest(string AccessToken, string RefreshToken);
