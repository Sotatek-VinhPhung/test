namespace CleanArchitecture.Application.Users.DTOs;

public record CreateUserRequest(string FirstName, string LastName, string Email, string Password);
