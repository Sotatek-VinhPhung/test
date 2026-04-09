namespace CleanArchitecture.Application.Users.DTOs;

/// <summary>
/// User data transfer object for API responses.
/// </summary>
public record UserDto(Guid Id, string FirstName, string LastName, string Email, string Role);
