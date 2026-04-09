using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Application.Users.DTOs;

namespace CleanArchitecture.Application.Users.Mappings;

/// <summary>
/// Manual mapping extensions — compile-time safe, no AutoMapper overhead.
/// </summary>
public static class UserMappings
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.Role.ToString());
}
